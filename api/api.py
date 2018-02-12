from time import sleep
import base64

from werkzeug.exceptions import MethodNotAllowed, RequestTimeout
from flask import jsonify, Response, g, abort, request
from flask_restful import Resource, Api
from flask_login import login_required, current_user

from models import *
from utils import ArgumentsParser
from login import check_login_user, token_valid


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
    (UserToken, '/get_token'),
    (UserTokenValid, '/is_token_valid'),
    (UserPassword, '/user/password'),
    (ApiCatchall, '/<path:path>/')
]
