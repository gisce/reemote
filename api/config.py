# -*- coding: utf-8 -*-
from flask import Flask, g
from api import REEmoteApi
# from metrics import Metrics
# from osconf import config_from_environment
from flask_login import LoginManager

from redis import Redis
from rq import Queue

api = REEmoteApi(prefix='/api/v1')

# metrics = Metrics()


def create_app(**config):
    """
    Create a OficinaVirtual app
    :param config: 
    :return: OficinaVirtual app
    """
    app = Flask(__name__, static_folder=None)
    app.config.update(config)

    if not app.config['SECRET_KEY']:
        app.config['SECRET_KEY'] = 'Very very very secret key!!!!!'

    if 'LOG_LEVEL' not in app.config:
        app.config['LOG_LEVEL'] = 'DEBUG'

    # configure_login(app)
    configure_api(app)
    configure_backend(app)
    # configure_metrics(app)

    return app


def configure_api(app):
    """
    Configure diffenrend API endpoints
    :param app: Flask application 
    :return: 
    """
    from api import resources
    for resource in resources:
        api.add_resource(*resource)

    api.init_app(app)


def setup_backend_conn():
    # **config_from_environment('REDIS')
    queue = Queue('calls', connection=Redis())
    g.queue = queue


def configure_backend(app):
    app.before_request(setup_backend_conn)


# def configure_metrics(app):
#     metrics.init_app(app)


# def configure_login(app):
#     from login import load_user_from_header, load_user, CustomSessionInterface
#     login_manager = LoginManager()
#     login_manager.init_app(app)
#     # Add request loader callback
#     login_manager.request_loader(load_user_from_header)
#     login_manager.user_loader(load_user)
#     app.session_interface = CustomSessionInterface()
