# -*- coding: utf-8 -*-

from subprocess import Popen, PIPE
import json
import logging
from datetime import datetime
from pytz import timezone
import os
try:
    from urllib.parse import urlparse
except ImportError:
     from urlparse import urlparse
import requests
import iec870ree.ip
import iec870ree.protocol
import iec870ree_moxa.moxa

TIMEZONE = timezone('Europe/Madrid')

logger = logging.getLogger(__name__)

MAGNITUDES = {
    1: 'AI',
    2: 'AE',
    3: 'R1',
    4: 'R2',
    5: 'R3',
    6: 'R4',
    7: 'RES7',
    8: 'RES8'
}

DEFAULT_CONTRACT_FLOW = {
    1: 'Import',
    2: 'Import',
    3: 'Export'
}


def validate(date_text):
    try:
        if date_text != datetime.strptime(date_text, '%Y-%m-%dT%H:%M:%S'
                                          ).strftime('%Y-%m-%dT%H:%M:%S'):
            raise ValueError
        return True
    except ValueError:
        return False


def get_season(dt):
    if dt.dst().seconds > 0:
        return 'S'
    else:
        return 'W'


def parse_billings(billings, contract, meter_serial, datefrom, dateto):
    res = {
        'Contract': contract,
        'DateFrom': datefrom,
        'DateTo': dateto,
        'Flow': DEFAULT_CONTRACT_FLOW[contract],
        'SerialNumber': str(meter_serial),
        'Totals': []
    }

    for billing_period in billings:
        period = {
            'Tariff': billing_period.address % 10,  # Get last digit of address
            'Excess': billing_period.excess_power,
            'MaximumDemandTimeStamp': billing_period.max_power_date.strftime('%Y-%m-%d %H:%M:%S'),
            'QualityMaximumDemand': billing_period.max_power_qual,
            'MaximumDemand': billing_period.max_power,
            'QualityReservedField8': billing_period.reserved_8_qual,
            'ReservedField8': billing_period.reserved_8,
            'QualityReservedField7': billing_period.reserved_7_qual,
            'ReservedField7': billing_period.reserved_7,
            'QualityReactiveCapacitiveEnergy': billing_period.reactive_qual_cap,
            'ReactiveCapacitiveEnergyInc': billing_period.reactive_inc_cap,
            'ReactiveCapacitiveEnergyAbs': billing_period.reactive_abs_cap,
            'QualityReactiveInductiveEnergy': billing_period.reactive_qua_ind,
            'ReactiveInductiveEnergyInc': billing_period.reactive_inc_ind,
            'ReactiveInductiveEnergyAbs': billing_period.reactive_abs_ind,
            'QualityActiveEnergy': billing_period.active_qual,
            'ActiveEnergyInc': billing_period.active_inc,
            'ActiveEnergyAbs': billing_period.active_abs,
            'PeriodEnd': billing_period.date_end.strftime('%Y-%m-%d %H:%M:%S'),
            'PeriodStart': billing_period.date_start.strftime('%Y-%m-%d %H:%M:%S'),
            'QualityExcess': billing_period.ecxess_power_qual
        }
        res['Totals'].append(period)
    return res


def parse_profiles(profiles, meter_serial, datefrom, dateto):
    res = {
        'Number': 1,
        'Absolute': False,
        'DateFrom': datefrom,
        'DateTo': dateto,
        'SerialNumber': str(meter_serial),
        'Records': []
    }
    for hour_profile in profiles:
        date = hour_profile[0].datetime
        record = {
            'TimeInfo': date.strftime('%Y-%m-%d %H:%M:%S'),
            'Season': get_season(TIMEZONE.localize(date)),
            'Channels': []
        }
        for channel in hour_profile:
            if channel.address not in [7, 8]:  # Skip RES7 and RES8 registers
                channel = {
                    'Magnitude': MAGNITUDES[channel.address],
                    'Value': channel.total,
                    'Quality': channel.quality
                }
                record['Channels'].append(channel)
        res['Records'].append(record)
    return res


class ReemoteTCPIPWrapper(object):

    def __init__(self, ipaddr, port, link, mpoint, passwrd, datefrom, dateto,
                 option, request, contract=None):
        """

        :param ipaddr: Ip addres for the connection
        :param port: Port for the connection
        :param link: LinkAddress
        :param mpoint: MeasuringPointAddress
        :param passwrd: Password
        :param datefrom: Date
        :param dateto: Date
        :param option: Either "b" for Billings or "p" for Profiles
        :param request: Different types of request for the Profiles
        :param contract: List of contracts e.g:[1,3]
        """
        if validate(datefrom) and validate(dateto):
            self.meter_serial = None
            self.app_layer = None
            self.physical_layer = None
            self.ipaddr = ipaddr
            self.port = port
            self.link = link
            self.mpoint = mpoint
            self.passwrd = passwrd
            self.datefrom = datetime.strptime(datefrom, '%Y-%m-%dT%H:%M:%S')
            self.dateto = datetime.strptime(dateto, '%Y-%m-%dT%H:%M:%S')
            self.option = option
            self.request = request
            if contract:
                if not isinstance(contract, list):
                    contract = list(contract)
                self.contract = contract
            else:
                self.contract = []

            if 'REEMOTE_PATH' in os.environ:
                self.reemote = urlparse(os.environ['REEMOTE_PATH'])
            else:
                self.reemote = 'local'
        else:
            raise ValueError(
                'ERROR: Date format is wrong. Expected: %Y-%m-%dT%H:%M:%S'
            )

    def handle_file_request(self):
        output = ''
        if self.app_layer:
            if self.meter_serial is None:
                resp = self.app_layer.get_info()
                self.meter_serial = resp.content.codigo_equipo
                print(self.meter_serial)
            if self.option == 'b':
                if not self.contract:
                    self.contract = [1, 2, 3]
                output = self.get_billings()
            elif self.option == 'p':
                output = self.get_profiles()
            elif self.option == 'p4':
                output = self.get_quarter_hour_profiles()
        result = {
            'error': True if not output else False,
            'message': '',
            'error_message': 'No output received',
        }
        if output:
            try:
                result['message'] = output
                result['error_message'] = ''
            except:
                result['error'] = True
                result['error_message'] = 'ERROR: No JSON object could be decoded'
        return result

    def execute_request(self):
        if self.reemote == 'local':
            self.establish_connection()
            if self.app_layer is not None and self.physical_layer is not None:
                result = self.handle_file_request()
                self.close_connection()
            else:
                return {
                    'error': True,
                    'message': '',
                    'error_message': "Couldn't establish connection",
                }

        elif self.reemote.scheme == 'http':
            logger.info('Sending request to API...')
            post_data = {
                'ipaddr': self.ipaddr,
                'port': self.port,
                'link': self.link,
                'mpoint': self.mpoint,
                'passwrd': self.passwrd,
                'datefrom': self.datefrom.strftime('%Y-%m-%dT%H:%M:%S'),
                'dateto': self.dateto.strftime('%Y-%m-%dT%H:%M:%S'),
                'option': self.option,
                'request': self.request,
                'contract': self.contract
            }
            response = requests.post(self.reemote.geturl(), data=post_data, allow_redirects=True)
            response = json.loads(response.content)
            if response['error']:
                logger.info('Received error message from API')
                result = {
                    'error': True,
                    'message': response['message'],
                    'error_message': response['errors'],
                }
            else:
                logger.info('Received data without error from API')
                result = {
                    'error': False,
                    'message': response['message'],
                    'id': response['id'],
                }
        else:
            result = {
                    'error': True,
                    'message': '',
                    'error_message': 'REEMOTE_PATH protocol unknown',
            }

        return result

    def get_billings(self):
        logger.info('Requesting billings to device.')
        res = {'Results': []}
        for contract in self.contract:
            values = []
            for resp in self.app_layer.stored_tariff_info(self.datefrom,
                                                          self.dateto,
                                                          register=contract):
                values.extend(resp.content.valores)
            aux = parse_billings(values, contract, self.meter_serial,
                                 self.datefrom.strftime('%Y-%m-%d %H:%M:%S'),
                                 self.dateto.strftime('%Y-%m-%d %H:%M:%S'))
            res['Results'].append(aux)
        return res

    def get_profiles(self):
        logger.info('Requesting profiles to device.')
        values = []
        for resp in self.app_layer.read_incremental_values(self.datefrom,
                                                           self.dateto,
                                                           register='profiles'):
            values.append(resp.content.valores)
        return parse_profiles(values, self.meter_serial,
                              self.datefrom.strftime('%Y-%m-%d %H:%M:%S'),
                              self.dateto.strftime('%Y-%m-%d %H:%M:%S'))

    def get_quarter_hour_profiles(self):
        logger.info('Requesting quarter hour profiles to device.')
        values = []
        for resp in self.app_layer.read_incremental_values(
                self.datefrom, self.dateto, register='quarter_hour'):
            values.append(resp.content.valores)
        return parse_profiles(values, self.meter_serial,
                              self.datefrom.strftime('%Y-%m-%d %H:%M:%S'),
                              self.dateto.strftime('%Y-%m-%d %H:%M:%S'))

    def establish_connection(self):
        try:
            logger.info('Establishing connection...')
            physical_layer = iec870ree.ip.Ip((self.ipaddr, self.port))
            link_layer = iec870ree.protocol.LinkLayer(self.link, self.mpoint)
            link_layer.initialize(physical_layer)
            app_layer = iec870ree.protocol.AppLayer()
            app_layer.initialize(link_layer)

            physical_layer.connect()
            logger.info('Physical layer connected')
            link_layer.link_state_request()
            link_layer.remote_link_reposition()
            logger.info('Authentication...')
            resp = app_layer.authenticate(self.passwrd)
            logger.info('CLIENT authentication response: {}'.format(resp))

            self.app_layer = app_layer
            self.physical_layer = physical_layer
        except Exception as e:
            logger.info('Connection failed. Exiting process...')
            if self.connected:
                self.close_connection()

    def close_connection(self):
        logger.info('Closing connection...')
        self.app_layer.finish_session()
        self.physical_layer.disconnect()
        logger.info('Disconnected')

    @property
    def connected(self):
        if self.physical_layer is None:
            return False
        else:
            return self.physical_layer.alive.is_set()

    def get_status(self, job_id):
        url = self.reemote.geturl() + "{}".format(job_id)
        response = requests.get(url)
        return json.loads(response.content)


class ReemoteMOXAWrapper(ReemoteTCPIPWrapper):

    def __init__(self, phone, ipaddr, port, link, mpoint, passwrd, datefrom,
                 dateto, option, request, contract=None):
        """
        :param phone: Phone number
        """
        self.phone = phone
        super(ReemoteMOXAWrapper, self).__init__(ipaddr, port, link, mpoint,
                                                 passwrd, datefrom, dateto,
                                                 option, request, contract)

    def establish_connection(self):
        try:
            logger.info('Establishing connection...')
            ip_layer = iec870ree.ip.Ip((self.ipaddr, self.port))
            moxa_layer = iec870ree_moxa.moxa.Moxa(self.phone, ip_layer)
            link_layer = iec870ree.protocol.LinkLayer(self.link, self.mpoint)
            link_layer.initialize(moxa_layer)
            app_layer = iec870ree.protocol.AppLayer()
            app_layer.initialize(link_layer)

            moxa_layer.connect()
            logger.info('MOXA physical layer connected')
            link_layer.link_state_request()
            link_layer.remote_link_reposition()
            logger.info('Authentication...')
            resp = app_layer.authenticate(self.passwrd)
            logger.info('CLIENT authentication response: {}'.format(resp))

            self.app_layer = app_layer
            self.physical_layer = moxa_layer
        except Exception as e:
            logger.info('Connection failed. Exiting process...')
            if self.connected:
                self.close_connection()

    def execute_request(self):
        if self.reemote == 'local':
            self.establish_connection()
            if self.app_layer is not None and self.physical_layer is not None:
                result = self.handle_file_request()
                self.close_connection()
            else:
                return {
                    'error': True,
                    'message': '',
                    'error_message': "Couldn't establish connection",
                }

        elif self.reemote.scheme == 'http':
            logger.info('Sending request to API...')
            post_data = {
                'phone': self.phone,
                'ipaddr': self.ipaddr,
                'port': self.port,
                'link': self.link,
                'mpoint': self.mpoint,
                'passwrd': self.passwrd,
                'datefrom': self.datefrom.strftime('%Y-%m-%dT%H:%M:%S'),
                'dateto': self.dateto.strftime('%Y-%m-%dT%H:%M:%S'),
                'option': self.option,
                'request': self.request,
                'contract': self.contract
            }
            response = requests.post(self.reemote.geturl(), data=post_data, allow_redirects=True)
            response = json.loads(response.content)
            if response['error']:
                logger.info('Received error message from API')
                result = {
                    'error': True,
                    'message': response['message'],
                    'error_message': response['errors'],
                }
            else:
                logger.info('Received data without error from API')
                result = {
                    'error': False,
                    'message': response['message'],
                    'id': response['id'],
                }
        else:
            result = {
                    'error': True,
                    'message': '',
                    'error_message': 'REEMOTE_PATH protocol unknown',
            }

        return result


class ReemoteModemWrapper(object):

    def __init__(self, phone, port, link, mpoint, passwrd, datefrom, dateto,
                 option, request, contract=None):
        """

        :param phone: Phone number of the modem
        :param port: Port for the connection
        :param link: LinkAddress
        :param mpoint: MeasuringPointAddress
        :param passwrd: Password
        :param datefrom: Date
        :param dateto: Date
        :param option: Either "b" for Billings or "p" for Profiles
        :param request: Different types of request for the Profiles
        :param contract: List of contracts e.g:[1,3]
        """
        if validate(datefrom) and validate(dateto):
            self.phone = phone
            self.port = port
            self.link = link
            self.mpoint = mpoint
            self.passwrd = passwrd
            self.datefrom = datefrom
            self.dateto = dateto
            self.option = option
            self.request = request
            if contract:
                if not isinstance(contract, list):
                    contract = list(contract)
                self.contract = contract
            else:
                self.contract = None

            if 'REEMOTE_PATH' in os.environ:
                if os.environ['REEMOTE_PATH'] == 'local':
                    self.reemote = 'local'
                else:
                    self.reemote = urlparse(os.environ['REEMOTE_PATH'])
            else:
                raise ValueError('Can\'t find the REEMOTE_PATH variable')
        else:
            raise ValueError(
                'ERROR: Date format is wrong. Expected: %Y-%m-%dT%H:%M:%S'
            )

    def execute_request(self):
        protocol = self.reemote.scheme

        if protocol == 'file':
            command = "mono {0} -n {1} -o {2} -l {3} -m {4} -w {5} " \
                    "-f {6} -t {7}".format(self.reemote.path, self.phone, self.port,
                                           self.link, self.mpoint, self.passwrd,
                                           self.datefrom, self.dateto)
            result = self.handle_file_request(command)

        elif protocol == 'http':
            post_data = {
                'phone': self.phone,
                'port': self.port,
                'link_address': self.link,
                'mpoint': self.mpoint,
                'passwrd': self.passwrd,
                'datefrom': self.datefrom,
                'dateto': self.dateto,
                'option': self.option,
                'request': self.request,
                'contract': self.contract
            }
            response = requests.post(self.reemote.geturl(), data=post_data)
            if response['error']:
                result = {
                    'error': True,
                    'message': response['message'],
                    'error_message': response['errors'],
                }
            else:
                result = {
                    'error': False,
                    'message': response['message'],
                    'id': response['id'],
                }
        else:
            result = {
                    'error': True,
                    'message': '',
                    'error_message': 'REEMOTE_PATH protocol unknown',
            }
        return result

    def handle_file_request(self, command):
        if self.option == "b":
                command += " -b"
                if self.contract:
                    for contract in self.contract:
                        command += " -c{}".format(contract)
                else:
                    command += " -c1 -c2 -c3"
        elif self.option == "p":
            command += " -p -r {0}".format(self.request)

        proc = Popen(command.split(), stdout=PIPE, stderr=PIPE)
        stdout, stderr = proc.communicate()
        result = {
            'error': True if stderr else False,
            'message': '',
            'error_message': stderr,
        }
        if stdout:
            try:
                result['message'] = json.loads(stdout)
            except:
                if result['error']:
                    result['error_message'] = '{} {}'.format(stderr, 'ERROR: No JSON object could be decoded')
                else:
                    result['error'] = True
                    result['error_message'] = 'ERROR: No JSON object could be decoded'
        return result
