# -*- coding: utf-8 -*-

from subprocess import Popen, PIPE
import json
import logging
from datetime import datetime

logging.basicConfig(format='%(asctime)s %(message)s',
                    datefmt='[%Y-%m-%dT%H:%M:%S]',
                    level=logging.INFO)


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
                 option, request, contract):
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
            self.contract = contract
        else:
            logging.getLogger(__name__).info(
                'ERROR: Date format is wrong. Expected: %Y-%m-%dT%H:%M:%S'
            )
    
    def execute_request(self):
        command = "mono reemote.exe -i {0} -o {1} -l {2} -m {3} -w {4} " \
                  "-f {5} -t {6}".format(self.ipaddr, self.port, self.link,
                                         self.mpoint, self.passwrd,
                                         self.datefrom, self.dateto)
        if self.option == "b":
            command += " -b"
        elif self.option == "p":
            command += " -p -r {0} -c {1}".format(self.request, self.contract)

        proc = Popen(command.split(), stdout=PIPE, stderr=PIPE)
        stdout, stderr = proc.communicate()
        json.loads(stdout)
