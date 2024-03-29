# -*- coding: utf-8 -*-
__author__ = 'XaviTorello'

import sys
sys.path.insert(0, '.')

from flask import json
from app import application

import logging
#logging.basicConfig(level=logging.DEBUG)

# Python 2 and python3 compat for str type assertions
try:
  basestring
except NameError:
  basestring = str

base_url = "/api/v1"

mandatory_main_elements = {
    "error": bool,
    "message": basestring,
    "id": basestring,
}


request_to_perform = {
    'call': {
        'ipaddr': "81.33.218.21",
        'port': 55,
        'link': 3,
        'mpoint': 5,
        'passwrd': 5,
        'datefrom': "2017-01-01T00:00:00",
        'dateto': "2017-01-02T00:00:00",
        'option': 5,
        'request': 5,
    }
}


def assert_response_main_fields(response):
    """
    Validate the default fields existant in any response:
    - items
    - n_items
    - limit
    - offset
    """

    for element, expected_type in mandatory_main_elements.items():
        assert element in response, "'{}' is not present in the response '{}'".format(element, response.keys())
        assert isinstance(response[element], expected_type), "'{}' is not present in the response '{}'".format(element, response.keys())
    return True


with description('API'):

    with before.each:
        self.app = application.test_client()

    with context('Call'):
        with context(''):
            with before.each:
                self.url = base_url + "/call/"

                self.parameters = request_to_perform['call']

            with it('must work as expected'):
                response = self.app.post(self.url, data=self.parameters, follow_redirects=True)
                assert response.status_code == 200

                result = json.loads(response.data)
                assert assert_response_main_fields(result)

                assert not result['error'], "Call enqueue must work!"

                job_id = result['id']
                assert job_id, "Job ID must be fetched"

                # Second part, fetch the status of call
                self.url = base_url + "/call/{}".format(job_id)
                response = self.app.get(self.url, follow_redirects=True)
                assert response.status_code == 200

                result = json.loads(response.data)
                print (result)
                assert assert_response_main_fields(result)
                assert not result['error'], "Call status must be fetched without errors!"
