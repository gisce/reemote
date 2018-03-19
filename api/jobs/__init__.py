# -*- coding: utf-8 -*-

def call_using_custom_wrapper(wrapper, params):
    print (wrapper)
    print (params)
    wrapper(**params)
