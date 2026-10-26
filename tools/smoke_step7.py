"""Integration smoke test against a running Development API; no database required.
Uses locally signed test tokens to check route policies, NOT the login/account lifecycle.
Run only against your own local instance with its development signing key.
"""
import base64
import hashlib
import hmac
import json
import os
from pathlib import Path
import sys
import time
import urllib.error
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
BASE = sys.argv[1] if len(sys.argv) > 1 else 'http://127.0.0.1:5087'
settings = ROOT / 'TPN°5-Movil' / 'appsettings.Development.json'
jwt = json.loads(settings.read_text()).get('Jwt', {}) if settings.exists() else {}
key = os.environ.get('Jwt__Key', jwt.get('Key'))
issuer = os.environ.get('Jwt__Issuer', jwt.get('Issuer'))
audience = os.environ.get('Jwt__Audience', jwt.get('Audience'))
if not all((key, issuer, audience)):
    raise SystemExit('Configure local development Jwt settings; never paste secret values in output.')

def b64(data):
    return base64.urlsafe_b64encode(data).rstrip(b'=')

def token(role):
    now = int(time.time())
    header = b64(json.dumps({'alg': 'HS256', 'typ': 'JWT'}).encode())
    payload = b64(json.dumps({'sub': '1', 'role': role, 'iss': issuer, 'aud': audience,
                              'nbf': now - 5, 'exp': now + 120}).encode())
    signed = header + b'.' + payload
    return (signed + b'.' + b64(hmac.new(key.encode(), signed, hashlib.sha256).digest())).decode()

checks = 0

def request(path, expected, role=None, body=None, method=None, headers=None):
    global checks
    hdr = dict(headers or {})
    if role:
        hdr['Authorization'] = 'Bearer ' + token(role)
    data = None
    if body is not None:
        data = json.dumps(body).encode()
        hdr['Content-Type'] = 'application/json'
    req = urllib.request.Request(BASE + path, data=data, headers=hdr, method=method)
    try:
        response = urllib.request.urlopen(req, timeout=15)
    except urllib.error.HTTPError as exc:
        response = exc
    with response:
        data = response.read()
        assert response.code == expected, f'{path}: expected {expected}, got {response.code}'
        if expected == 501:
            assert 'error' in json.loads(data)
        checks += 1
        print(f'PASS {req.get_method()} {path}: {expected}')
        return data, response.headers

raw, _ = request('/swagger/v1/swagger.json', 200)
swagger = json.loads(raw)
assert len(swagger['paths']) == 22
schemas = swagger['components']['schemas']
assert 'detalles' in schemas['IngresoResponse']['properties']
assert 'detalles' in schemas['SalidaResponse']['properties']
assert 'proveedorId' not in schemas['ProductoCreateRequest']['properties']
assert 'clienteId' not in schemas['SalidaRequest']['properties']
assert 'rol' not in schemas['UsuarioUpdateRequest']['properties']
request('/swagger/index.html', 200)
request('/api/auth/me', 401)
request('/api/productos', 401)
request('/api/usuarios', 403, role='Empleado')
request('/api/clientes', 403, role='Cliente')
request('/api/productos/1/movimientos', 403, role='Cliente')
request('/api/salidas', 403, role='Cliente')  # Ownership workflow is not implemented yet.
request('/api/usuarios', 403, role='Admin')  # Previous role name must not be accepted.
request('/api/ingresos', 501, role='Empleado', body={'proveedorId': 1, 'observaciones': 'Smoke'})
request('/api/salidas', 501, role='Empleado', body={'observaciones': 'Smoke'})
request('/api/usuarios', 501, role='Administrador', body={
    'nombre': 'Smoke', 'apellido': 'Test', 'cuit': 'test', 'email': 'smoke@example.invalid',
    'password': 'Local-test-only', 'rol': 'Cliente'})
request('/api/clientes', 501, role='Administrador', body={
    'nombre': 'Smoke', 'apellido': 'Test', 'cuit': 'test', 'email': 'smoke@example.invalid'})
request('/api/auth/register', 501, role='Administrador', body={
    'nombreCompleto': 'Smoke Test', 'email': 'smoke@example.invalid',
    'password': 'Local-test-only', 'rol': 'Cliente'})
_, headers = request('/api/productos', 204, method='OPTIONS', headers={
    'Origin': 'http://example.invalid', 'Access-Control-Request-Method': 'GET'})
assert headers.get('Access-Control-Allow-Origin') == '*'
print(f'{checks} HTTP checks passed; Swagger contracts and permissive CORS verified.')
