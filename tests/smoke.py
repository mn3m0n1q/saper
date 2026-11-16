import json, sys, urllib.request, urllib.error
base = sys.argv[1] if len(sys.argv)>1 else 'http://localhost:5080'
def post(path, body, expected=200):
    req=urllib.request.Request(base+path, data=json.dumps(body).encode(), headers={'Content-Type':'application/json'})
    try: response=urllib.request.urlopen(req)
    except urllib.error.HTTPError as e: response=e
    assert response.status==expected, (response.status, expected)
    return json.load(response)
for level,w,h,m in [('easy',9,9,10),('medium',16,16,40),('hard',30,16,99)]:
    game=post('/api/games/', {'level':level})
    assert (game['width'], game['height'], game['mines'])==(w,h,m)
    path='/api/games/'+game['id']+'/moves'
    post(path, {'index':-1,'action':'reveal'},400)
    post(path, {'index':w*h,'action':'reveal'},400)
    post(path, {'index':0,'action':'invalid'},400)
    game=post(path, {'index':0,'action':'flag'}); assert game['flags']==1
    game=post(path, {'index':0,'action':'reveal'}); assert game['status']=='ready'
    post(path, {'index':0,'action':'flag'})
    game=post(path, {'index':0,'action':'reveal'})
    assert game['cells'][0]['revealed'] and game['cells'][0]['adjacent']==0
    assert all(c['mine'] is None for c in game['cells'])
post('/api/games/', {'level':'invalid'},400)
post('/api/games/00000000-0000-0000-0000-000000000000/moves', {'index':0,'action':'reveal'},404)
assert urllib.request.urlopen(base+'/health').status==200
assert urllib.request.urlopen(base+'/').status==200
print('PASS: HTTP, levels, flagging, safe start, hidden mines, invalid requests, missing game.')
