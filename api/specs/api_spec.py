# -*- coding: utf-8 -*-
__author__ = 'XaviTorello'

import sys
sys.path.insert(0, '.')

from flask import json
from app import application

import logging
#logging.basicConfig(level=logging.DEBUG)


base_url = "/api/v1"

mandatory_main_elements = {
    "error": bool,
    "message": str,
    "id": str,
}


request_to_perform = {
    'call': {
        'ip': "81.33.218.290",
        'port': 55,
        'link_address': 3,
        'mpoint': 5,
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
        assert type(response[element]) == expected_type, "'{}' is not present in the response '{}'".format(element, response.keys())
    return True


with description('API'):

    with before.each:
        self.app = application.test_client()

    with context('Call'):
        with context(''):
            with before.each:
                self.url = base_url + "/call/"

                self.parameters = request_to_perform['call']
