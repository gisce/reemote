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

class IPCallSchema(CallSchema):
    ipaddr = fields.String()

class NumberCallSchema(CallSchema):
    phone = fields.String()
