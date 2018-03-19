# -*- coding: utf-8 -*-
from __future__ import (division, absolute_import, print_function, unicode_literals)

from werkzeug.exceptions import MethodNotAllowed
from flask import jsonify, Response, g, abort, request
from flask_restful import Resource, Api
from flask_login import login_required, current_user
# from login import check_login_user, token_valid

from pyreemote.telemeasure import ReemoteTCPIPWrapper, ReemoteModemWrapper

from schemas import IPCallSchema, NumberCallSchema
from jobs import call_using_custom_wrapper


class REEmoteApi(Api):
    pass


class BaseResource(Resource):
    pass


class ApiCatchall(BaseResource):

    def get(self, path):
        abort(404)

    post = get
    put = get
    delete = get
    patch = get


class ReadOnlyResource(BaseResource):

    def not_allowed(self):
        raise MethodNotAllowed

    post = patch = not_allowed


class SecuredResource(BaseResource):

    method_decorators = [login_required]


class CallEnqueue(Resource):

    def post(self):
        params = request.values.to_dict()

        try:
            # IP or Telephone number must exist
            assert 'ipaddr' in params or 'number' in params

            if 'ipaddr' in params:
                assert type(params['ipaddr']) in [str, unicode] and params['ipaddr'] != "", \
                    "IP address '{}' is not correct".format(params['ipaddr'])
                remote_wrapper = ReemoteTCPIPWrapper
                schema = IPCallSchema

            else:
                assert type(params['phone']) in [str, unicode] and params['phone'] != "",\
                    "Phone number '{}' is not correct".format(params['phone'])
                remote_wrapper = ReemoteModemWrapper
                schema = NumberCallSchema

            call_params, validation_errors = schema().load(params)

        except AssertionError as e:
            response = jsonify({
                "error": True,
                "message": "Error fetching call parameters",
                'errors': {'parameter': e.args[0]},
            })
            response.status_code = 422
            return response

        job = g.queue.enqueue(call_using_custom_wrapper, remote_wrapper,
                              call_params)

        if job:
            return jsonify({
                "error": False,
                "id": job.id,
                "message": "Job enqueued",
            })

        abort(500)


class CallStatus(Resource):

    def get(self, job_id):

        try:
            assert type(job_id) in [str, unicode] and job_id, \
                "Job ID '{}' is not correct".format(job_id)

        except AssertionError as e:
            response = jsonify({
                "error": True,
                "message": "Error fetching job id",
                'errors': {'parameter': e.args[0]},
            })
            response.status_code = 422
            return response

        job = g.queue.fetch_job(job_id)

        if job:
            return jsonify({
                "error": False,
                "id": job.id,
                "status": {
                    "state": job.status,
                    "description": job.description,
                    "ttl": job.ttl,
                    "timeout": job.timeout,
                    "result": job.result,
                },
                "message": "Job status fetched",
            })

        # Handle error
        response = jsonify({
            "error": True,
            "id": job.id,
            "message": "Job '{}' not found".format(job_id),
        })
        response.status_code = 500
        return response


class UserToken(Resource):
    def post(self):
        g.login_via_header = True
        token = check_login_user(**request.json)
        return jsonify({
            'token': token
        })


class UserTokenValid(Resource):
    def post(self):
        # Don't use cookies
        g.login_via_header = True
        user = token_valid(request.json.get('token'))
        return jsonify({
            'token_is_valid': user is not None
        })


class User(ReadOnlyResource, SecuredResource):
    def get(self):
        model = UserModel()


class UserPassword(SecuredResource):
    def put(self):
        values = request.json
        current = values.get('current')
        password = values.get('password')
        state = current_user.change_password(current, password)
        if state:
            return jsonify({
                    "status": "OK"
                })
        return jsonify({
                "status_code": 403,
                "status": "ERROR",
                "errors": {
                    "grant": "User not found or incorrect password"
                }
            })


resources = [
    (CallEnqueue, '/call/'),
    (CallStatus, '/call/<string:job_id>'),
    (UserToken, '/get_token'),
    (UserTokenValid, '/is_token_valid'),
    (UserPassword, '/user/password'),
    (ApiCatchall, '/<path:path>/')
]
