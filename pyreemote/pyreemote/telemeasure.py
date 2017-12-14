# -*- coding: utf-8 -*-

from subprocess import Popen, PIPE
import json
import logging
from datetime import datetime
import os


logger = logging.getLogger(__name__)


def validate(date_text):
    try:
        if date_text != datetime.strptime(date_text, '%Y-%m-%dT%H:%M:%S'
                                          ).strftime('%Y-%m-%dT%H:%M:%S'):
            raise ValueError
        return True
    except ValueError:
        return False


class ReemoteWrapper(object):

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
        :param option: Wither "b" for Billing or "p" for Profiles
        :param request: Different typres of request for the Profiles
        :param contract: List of contracts e.g:[1,3]
        """
        if validate(datefrom) and validate(dateto):
            self.ipaddr = ipaddr
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
                self.reemote = os.environ['REEMOTE_PATH']
                if not os.path.exists(self.reemote):
                    raise ValueError('The designed path for the executable'
                                     ' doesn\'t exist')
            else:
                raise ValueError('Can\'t find the path to the Reemote '
                                 'executable')
        else:
            logger.info(
                'ERROR: Date format is wrong. Expected: %Y-%m-%dT%H:%M:%S'
            )
    
    def execute_request(self):
        command = "mono {0} -i {1} -o {2} -l {3} -m {4} -w {5} " \
                  "-f {6} -t {7}".format(self.reemote, self.ipaddr, self.port,
                                         self.link, self.mpoint, self.passwrd,
                                         self.datefrom, self.dateto)
        if self.option == "b":
            command += " -b"
            if self.contract:
                for contract in self.contract:
                    command += " -c{}".format(contract)
            else:
                command += " -c1 -c2 -c3"
        elif self.option == "p":
            command += " -p -r {0}".format(self.request)
        logger.info(
            'Command that will be executed: {}'.format(command)
        )
        proc = Popen(command.split(), stdout=PIPE, stderr=PIPE)
        stdout, stderr = proc.communicate()
        if stderr:
            logger.info(
                "ERROR: {}".format(stderr)
            )
        if stdout:
            return json.loads(stdout)
        else:
            return {}
