# -*- coding: utf-8 -*-
from werkzeug.exceptions import MethodNotAllowed
from flask import jsonify, Response, g, abort, request
from flask_restful import Resource, Api
from flask_login import login_required, current_user
# from login import check_login_user, token_valid

from pyreemote.telemeasure import ReemoteWrapper

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
            assert 'ip' in params or 'telephone_number' in params

            if 'ip' in params:
                assert type(params['ip']) == str and params['ip'] != "", "IP address '{}' is not correct".format(params['ip'])
                # remote_wrapper = IPWrapper
                remote_wrapper = ReemoteWrapper
                schema = IPCallSchema

            else:
                assert type(params['phone']) == str and params['phone'] != "", "Phone number '{}' is not correct".format(params['phone'])
                remote_wrapper = ReemoteWrapper
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

        job = g.queue.enqueue(call_using_custom_wrapper, remote_wrapper, call_params)

        if job:
            return jsonify({
                "error": False,
                "id": job.id,
                "message": "Job enqueued",
            })

        abort(500)


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
    (UserToken, '/get_token'),
    (UserTokenValid, '/is_token_valid'),
    (UserPassword, '/user/password'),
    (ApiCatchall, '/<path:path>/')
]
