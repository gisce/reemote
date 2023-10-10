# -*- coding: utf-8 -*-
from __future__ import (division, absolute_import, print_function, unicode_literals)

from werkzeug.exceptions import MethodNotAllowed
from flask import jsonify, Response, g, abort, request
from flask_restful import Resource, Api
from flask_login import login_required, current_user
# from login import check_login_user, token_valid

from pyreemote.telemeasure import ReemoteTCPIPWrapper, ReemoteModemWrapper, \
    ReemoteMOXAWrapper

from schemas import IPCallSchema, NumberCallSchema, MOXACallSchema
from jobs import call_using_custom_wrapper

from redis import Redis
import os
try:
    from importlib.metadata import version
except:
    from importlib_metadata import version
import socket
import time

# Python 2 and python3 compat for str type assertions
try:
  basestring
except NameError:
  basestring = str


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
        params = request.values.to_dict(flat=False)
        params = {k: params[k][0] if len(params[k]) <= 1 else params[k] for k in params}
        if params.get('contract') and not isinstance(params['contract'], list):
            params['contract'] = [params['contract']]
        try:
            # IP or Telephone number must exist
            assert 'ipaddr' in params or 'phone' in params
            if 'ipaddr' in params and 'phone' in params:
                assert isinstance(params['ipaddr'], basestring) and \
                       params['ipaddr'] != "", "IP address '{}' is not correct"\
                                                       .format(params['ipaddr'])
                assert isinstance(params['phone'], basestring) and \
                       params['phone'] != "", "Phone number '{}' is not correct" \
                    .format(params['phone'])
                remote_wrapper = ReemoteMOXAWrapper
                schema = MOXACallSchema
            elif 'ipaddr' in params:
                assert isinstance(params['ipaddr'], basestring) and \
                       params['ipaddr'] != "", "IP address '{}' is not correct"\
                                                       .format(params['ipaddr'])
                remote_wrapper = ReemoteTCPIPWrapper
                schema = IPCallSchema

            else:
                assert isinstance(params['phone'], basestring) and \
                      params['phone'] != "", "Phone number '{}' is not correct"\
                                                        .format(params['phone'])
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
                              call_params, job_timeout=3600)

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
            assert isinstance(job_id, basestring) and job_id, \
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
                    "state": job.get_status(),
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


class TestConnection(Resource):
    def get(self):
        redis_name = os.environ.get('REDIS_NAME', "")
        r = Redis(redis_name)
        try:
            r.ping()
            is_alive = True
        except Exception as e:
            is_alive = False
            print(repr(e))
        finally:
            r.close()

        return jsonify({
            'redis_ok': is_alive,
            'iec870ree': version('iec870ree'),
            'iec870ree_moxa': version('iec870ree_moxa')
        })


class SendMoxa(Resource):
    def get(self):
        address = request.args.get('address', False)
        commands = request.args.get('commands', False)

        if address:
            ip, port = address.split(':')
        else:
            return 'Must define address parameter.'
        sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        try:
            sock.settimeout(5)
            sock.connect((ip, int(port)))

            split_write_sleep = commands.split(';')
            res = []
            instruction_count = 0
            for write_sleep, i in enumerate(split_write_sleep):
                command, sleep = write_sleep.split(':')
                sock.sendall(bytes(command + '\r\n', 'ascii'))
                tmp_res = ''
                while 'OK' not in tmp_res and 'ERR' not in tmp_res:
                    print('receiving')
                    data = sock.recv(16)
                    tmp_res += str(data)
                    print('received {}'.format(tmp_res))
                res.append({'id': i+1, 'command': command, 'res': tmp_res})
                time.sleep(int(sleep))

        except Exception as e:
            res = str(e)
        finally:
            sock.close()

        return jsonify(res)


resources = [
    (CallEnqueue, '/call/'),
    (CallStatus, '/call/<string:job_id>'),
    (UserToken, '/get_token'),
    (UserTokenValid, '/is_token_valid'),
    (UserPassword, '/user/password'),
    (ApiCatchall, '/<path:path>/'),
    (TestConnection, '/test'),
    (SendMoxa, '/send')
]
