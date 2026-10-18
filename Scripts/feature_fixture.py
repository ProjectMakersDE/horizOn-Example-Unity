#!/usr/bin/env python3
"""Actual native game/Hello consumers, two process runs and exact local HTTP only."""
import argparse,base64,json,subprocess,threading,uuid
from pathlib import Path
from datetime import datetime,timezone,timedelta
from http.server import BaseHTTPRequestHandler,ThreadingHTTPServer
from urllib.parse import urlsplit
U='00000000-0000-4000-8000-000000000001';V='00000000-0000-4000-8000-000000000002';E='message:aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa';I='bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb';L='cccccccc-cccc-4ccc-8ccc-cccccccccccc';C='dddddddd-dddd-4ddd-8ddd-dddddddddddd';R=9007199254740000
T=datetime.now(timezone.utc).isoformat(timespec='milliseconds').replace('+00:00','Z');OLD=(datetime.now(timezone.utc)-timedelta(days=2)).isoformat().replace('+00:00','Z')
def friends(revision):return dict(userId=U,revision=revision,friendCode='H-'+base64.urlsafe_b64encode(uuid.UUID(U).bytes).decode().rstrip('='),friends=[],incoming=[],outgoing=[],blocked=[],limits=dict(maxFriends=20,maxIncomingRequests=20,maxOutgoingRequests=20,maxBlocked=100),serverTime=T)
def values():return dict(userId=U,revision=1,definitionVersion=1,day=T[:10],values=[dict(key='coins',balance=50,earnedToday=0,dailyCap=None)],operationWindow=dict(serverTime=T,oldestAcceptedAt=OLD))
def inbox(claimed=False):return dict(userId=U,cursor='claimed'if claimed else'original',revision=2 if claimed else 1,broadcastRevision=1,serverTime=T,pollAfterSeconds=120,unchanged=False,entries=[dict(id=E,kind='message',type='marketplace',reference=L,textKey='marketplace.item_return',parameters={},createdAt=T,expiresAt=OLD,overdue=True,read=claimed,claimed=claimed,attachment=dict(values=[],itemReturn=dict(listingId=L,instanceId=I,itemKey='hat')))],counts=dict(marketplace=dict(total=1,unread=0 if claimed else 1,claimable=0 if claimed else 1)),limits=dict(targeted=1,broadcasts=1,freeTexts=1,freeTextBytes=256,freeTextRetentionDays=7))
def inventory():return dict(userId=U,revision=1,instances=[dict(instanceId=I,itemKey='hat',properties={'label':'雪\0tail','power':9007199254740991},equipped=False,createdAt=T)],limits=dict(catalog=6000,inventory=250,receipts=80),serverTime=T)
AUTH=dict(userId=U,displayName='Lab',email='lab@example.invalid',accessToken='synthetic-feature-session',type='EMAIL',authStatus='AUTHENTICATED')
CASES=[
 ('POST','/user-management/signin',None,AUTH,200),
 ('GET','/achievements',None,dict(userId=U,revision=0,definitionVersion='a'*64,definitions=[],progress=[],limits=dict(maxDefinitions=20,maxLifetimeKeys=500),serverTime=T),200),
 ('GET','/friends',None,friends(R),200),
 ('POST','/friends/request',dict(userId=U,targetUserId=V,expectedRevision=R),dict(code='UNAVAILABLE'),503),
 ('GET','/friends',None,friends(R+1),200),
 ('GET','/inbox',None,inbox(),200),
 ('POST','/inbox/claim',dict(userId=U,entryId=E),dict(code='UNAVAILABLE'),503),
 ('POST','/user-management/signin',None,AUTH,200),
 ('POST','/inbox/claim',dict(userId=U,entryId=E),dict(entryId=E,replayed=True,inbox=inbox(True),playerValues=values(),items=[dict(instanceId=I,itemKey='hat')]),200),
 ('GET','/items',None,inventory(),200),
 ('POST','/gift-codes/validate',dict(userId=U,code='GIFT',includeIntent=True),dict(valid=True,protocolVersion=2,campaignId=C,expectedCount='9007199254740993',configRevision='9223372036854775806'),200),
 ('POST','/gift-codes/redeem','gift',dict(code='UNAVAILABLE'),503),
 ('POST','/gift-codes/redeem','gift',dict(success=True,alreadyApplied=True,replayed=True,campaignId=C,count='9007199254740994',configRevision='9223372036854775806',giftData=None,grantedValues=[],grantedUnlocks=[],playerValues=None),200)]
# Shared transport safely retries these permanent original identities four times.
for suffix in ['/inbox/claim','/gift-codes/redeem']:
 index=next(i for i,c in enumerate(CASES)if c[1]==suffix and c[4]==503)
 CASES[index:index+1]=[CASES[index]]*4
records=[];auth_checks=[];errors=[];original=None
class Handler(BaseHTTPRequestHandler):
 def log_message(self,*_):pass
 def do_GET(self):self.handle_case()
 def do_POST(self):self.handle_case()
 def handle_case(self):
  global original
  try:
   path=urlsplit(self.path).path;raw=self.rfile.read(int(self.headers.get('Content-Length','0')));body=json.loads(raw)if raw else None
   if path=='/api/v1/app/user-management/check-auth':
    assert body==dict(userId=U,sessionToken='synthetic-feature-session')
    auth_checks.append(dict(path=path,valid=True));reply=dict(userId=U,isAuthenticated=True,authStatus='AUTHENTICATED');status=200
   elif path.startswith('/api/v1/app/crash-reports/'):
    reply=dict(success=True,sessionId='synthetic-crash-session');status=200
   else:
    n=len(records);assert n<len(CASES),'Unexpected extra request '+path
    method,suffix,expected,reply,status=CASES[n];assert self.command==method and path=='/api/v1/app'+suffix,(n,self.command,path,suffix)
    assert self.headers.get('X-API-Key')=='feature-consumer-fixture'
    if suffix!='/user-management/signin':assert self.headers.get('Authorization')=='Bearer synthetic-feature-session'
    if expected=='gift':
     assert body==dict(userId=U,protocolVersion=2,code='GIFT',campaignId=C,expectedCount='9007199254740993',configRevision='9223372036854775806')
     if original is None:original=raw
     else:assert raw==original,'Gift retry changed original bytes'
    elif expected is not None:assert body==expected,(suffix,body,expected)
    records.append(dict(method=method,path=path,body=body))
   data=json.dumps(reply,ensure_ascii=True,separators=(',',':')).encode();self.send_response(status);self.send_header('Content-Type','application/json');self.send_header('Content-Length',str(len(data)));self.end_headers();self.wfile.write(data)
  except Exception as error:
   errors.append(str(error));self.send_error(500)
def main():
 p=argparse.ArgumentParser();p.add_argument('--player',type=Path,required=True);p.add_argument('--output',type=Path,required=True);p.add_argument('--render',action='store_true');a=p.parse_args();a.output.mkdir(parents=True,exist_ok=True)
 with ThreadingHTTPServer(('127.0.0.1',0),Handler)as server:
  thread=threading.Thread(target=server.serve_forever,daemon=True);thread.start();url=f'http://127.0.0.1:{server.server_port}'
  try:
   for stage,flag,marker in [('before-restart','-seagull-feature-stop-before-restart','RESTART_READY'),('after-restart','-seagull-feature-resume','[CURRENT FEATURE NATIVE] PASS')]:
    log=a.output/(stage+'.log');command=[str(a.player),* ([] if a.render else ['-batchmode']),'-screen-width','1280','-screen-height','720','-seagull-feature-fixture='+url,flag,'-logFile',str(log),'-seagull-capture='+str(a.output/(stage+'.png'))]
    result=subprocess.run(command,timeout=150);text=log.read_text(errors='replace');assert result.returncode==0 and marker in text,(stage,result.returncode,text[-4000:]);assert not errors,errors
  finally:
   server.shutdown()
   (a.output/'http-observed.json').write_text(json.dumps(dict(requests=records,cachedSessionChecks=auth_checks,errors=errors),indent=2)+'\n')
 assert len(records)==len(CASES),(len(records),len(CASES))
 if a.render:
  capture=a.output/'after-restart.png';assert capture.is_file() and capture.read_bytes().startswith(b'\x89PNG\r\n\x1a\n'),'Actual rendered feature UI capture absent'
 (a.output/'http-proof.json').write_text(json.dumps(dict(passed=True,requests=records,uncertainGiftBytesIdentical=True,actualProcessRestart=True,cachedSessionChecks=auth_checks,overdueEscrowClaimPreserved=True,rendered=a.render),indent=2)+'\n')
 print(f'CURRENT_FEATURE_NATIVE_PASS: {len(records)} exact HTTPs, real process restart, original Friends/Inbox/Gift identities, overdue escrow and current authoritative inventory')
if __name__=='__main__':main()
