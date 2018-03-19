# -*- coding: utf-8 -*-
from marshmallow import Schema, fields

class CallSchema(Schema):
    id = fields.Int()
    name = fields.Str()

    port = fields.Int()
    link_address = fields.Int()
    mpoint = fields.Int()
    password = fields.Int()
    date_from = fields.String()
    date_to = fields.String()
    option = fields.Str()
    request = fields.String()
    contract = fields.List(fields.Int)

class IPCallSchema(CallSchema):
    ipaddr = fields.String()

class NumberCallSchema(CallSchema):
    phone = fields.String()
