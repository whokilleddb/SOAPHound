#!/usr/bin/env python3
import os
import json
import base64
from datetime import datetime
from flask import Flask, request

app = Flask(__name__)
BUILD_DIR = 'build/'

def parsejson(data):
    json_object = json.loads(data)
    payload = json.dumps(json_object, indent=2)
    return payload

def recored(filename, content):
    try:
        # Try to create the directory
        os.makedirs(BUILD_DIR)
        print(f"* Directory '{BUILD_DIR}' created successfully.")
    except FileExistsError:
        # If the directory already exists, just print a message
        pass
    now = datetime.now()
    _f = os.path.join(BUILD_DIR, f'{now.strftime("%Y_%m_%d_%H_%M_%S")}_{filename}')
    f = open(_f, 'w') 
    f.write(content.strip())
    f.close()
    print(f"* Payload saved: {_f}")

@app.route('/')
def index():
    return {'status': 'ok'}

@app.route('/DNSDump', methods=['POST'])
def dnsdump():
    data = request.data.decode()
    recored("DNSdump.txt", base64.b64decode(data).decode())
    return {'status': 'ok'}

@app.route('/outputUsers', methods=['POST'])
def output_users():
    payload = parsejson(request.data.decode())
    recored("outputUser.json", payload)
    return {'status': 'ok'}

@app.route('/outputComputers', methods=['POST'])
def output_computers():
    payload = parsejson(request.data.decode())
    recored("outputComputers.json", payload)
    return {'status': 'ok'}

@app.route('/outputGroups', methods=['POST'])
def output_groups():
    payload = parsejson(request.data.decode())
    recored("outputGroups.json", payload)
    return {'status': 'ok'}

@app.route('/outputDomains', methods=['POST'])
def output_domains():
    payload = parsejson(request.data.decode())
    recored("outputDomains.json", payload)
    return {'status': 'ok'}

@app.route('/outputGPOs', methods=['POST'])
def output_gpos():
    payload = parsejson(request.data.decode())
    recored("outputGPOs.json", payload)
    return {'status': 'ok'}

@app.route('/outputOUs', methods=['POST'])
def output_ous():
    payload = parsejson(request.data.decode())
    recored("outputOUs.json", payload)
    return {'status': 'ok'}

@app.route('/outputContainers', methods=['POST'])
def output_containers():
    payload = parsejson(request.data.decode())
    recored("outputContainers.json", payload)
    return {'status': 'ok'}

@app.route('/CA', methods=['POST'])
def output_ca():
    payload = parsejson(request.data.decode())
    recored("CA.json", payload)
    return {'status': 'ok'}

@app.route('/CATemplate', methods=['POST'])
def output_catemplate():
    payload = parsejson(request.data.decode())
    recored("CATemplate.json", payload)
    return {'status': 'ok'}
