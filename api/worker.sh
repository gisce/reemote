#!/bin/bash
QUEUE="pyreemote-calls"

#rq worker -b $QUEUE
rq worker $QUEUE
