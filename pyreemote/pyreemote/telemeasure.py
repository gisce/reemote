# -*- coding: utf-8 -*-

from subprocess import Popen, PIPE
import json


class ReemoteWrapper(object):

    def __init__(self, ipaddr, port, link, mpoint, passwrd, datefrom, dateto,
                 option, request, contract):
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
