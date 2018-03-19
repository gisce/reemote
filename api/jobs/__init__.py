# -*- coding: utf-8 -*-

def call_using_custom_wrapper(wrapper, params):
    print ("Starting a new job")
    print (wrapper)
    print (params)
    return wrapper(**params).execute_request()
