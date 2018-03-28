# REEmote API

This provides a REST API interface for the REEmote library.

API specification can be reviewed at https://github.com/gisce/reemote/wiki/API

Two main components are involved:
- [The API app](#how-to-use-it)
- [The API worker](#api-worker)

## How to use it?

Install requirements:
```
$ pip install -r requirements.txt
```

Start the API with:
```
$ bash api.sh
```

, or (with tunned params at uwsgi init):
```
$ uwsgi api.ini --port X
```

, or (directly start Flask app):
```
$ python app.py
```

## API Worker

Start a worker to process call requests with:
```
$ bash worker.sh
```


## API definition

See the wiki for the API specification: https://github.com/gisce/reemote/wiki/API
