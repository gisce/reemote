# -*- coding: utf-8 -*-
from config import create_app


application = create_app()


if __name__ == '__main__':
    application.run(debug=False, use_reloader=False)
