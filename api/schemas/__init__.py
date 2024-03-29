# -*- coding: utf-8 -*-
from marshmallow import Schema, fields


class CallSchema(Schema):
    id = fields.Int()
    name = fields.Str()

    port = fields.Int()
    link = fields.Int()
    mpoint = fields.Int()
    passwrd = fields.Int()
    datefrom = fields.String()
    dateto = fields.String()
    option = fields.Str()
    request = fields.String()
    contract = fields.List(fields.Int)
    delay = fields.Int()
    wait_seconds = fields.Int()

class IPCallSchema(CallSchema):
    ipaddr = fields.String()


class NumberCallSchema(CallSchema):
    phone = fields.String()


class MOXACallSchema(CallSchema):
    ipaddr = fields.String()
    phone = fields.String()
    modem_init_str = fields.String()
