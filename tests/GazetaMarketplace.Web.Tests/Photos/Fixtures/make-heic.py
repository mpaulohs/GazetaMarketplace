import struct, sys
data = open('in.hevc','rb').read()
# split Annex B
nals=[]; i=0
import re
starts=[m.start() for m in re.finditer(b'\x00\x00\x01', data)]
for k,s in enumerate(starts):
    e = starts[k+1] if k+1<len(starts) else len(data)
    n = data[s+3:e]
    while n.endswith(b'\x00'): n=n[:-1]   # trailing zero bytes (start code prefix extra zero)
    nals.append(n)
by={}
for n in nals:
    t=(n[0]>>1)&0x3f
    by.setdefault(t,[]).append(n)
print({t:len(v) for t,v in by.items()})
vps,sps,pps=by[32][0],by[33][0],by[34][0]
slices=[n for n in nals if ((n[0]>>1)&0x3f)<32]
def box(t,payload): return struct.pack('>I',8+len(payload))+t+payload
def full(t,v,f,payload): return box(t,struct.pack('>I',(v<<24)|f)+payload)
# profile_tier_level from SPS (after 2 byte header + 1 byte)
ptl=sps[3:3+12]
hvcc=bytes([1])+ptl[0:1]+ptl[1:5]+ptl[5:11]+ptl[11:12]+struct.pack('>H',0xF000)+bytes([0xFC,0xFD,0xF8,0xF8])+struct.pack('>H',0)+bytes([0x0F,3])
for t,n in ((32,vps),(33,sps),(34,pps)):
    hvcc+=bytes([0x80|t])+struct.pack('>H',1)+struct.pack('>H',len(n))+n
w,h=64,48
ftyp=box(b'ftyp',b'heic'+struct.pack('>I',0)+b'mif1heic')
hdlr=full(b'hdlr',0,0,struct.pack('>I',0)+b'pict'+b'\0'*12+b'\0')
pitm=full(b'pitm',0,0,struct.pack('>H',1))
infe=full(b'infe',2,0,struct.pack('>HH',1,0)+b'hvc1'+b'\0')
iinf=full(b'iinf',0,0,struct.pack('>H',1)+infe)
ispe=full(b'ispe',0,0,struct.pack('>II',w,h))
hv=box(b'hvcC',hvcc)
ipco=box(b'ipco',ispe+hv)
ipma=full(b'ipma',0,0,struct.pack('>I',1)+struct.pack('>HB',1,2)+bytes([0x01,0x82]))
iprp=box(b'iprp',ipco+ipma)
payload=b''.join(struct.pack('>I',len(n))+n for n in slices)
def build(offset):
    iloc=full(b'iloc',0,0,bytes([0x44,0x00])+struct.pack('>H',1)+struct.pack('>HHH',1,0,1)+struct.pack('>II',offset,len(payload)))
    return box(b'meta',struct.pack('>I',0)+hdlr+pitm+iloc+iinf+iprp)
meta=build(0)
offset=len(ftyp)+len(meta)+8
meta=build(offset)
out=ftyp+meta+box(b'mdat',payload)
open('sample.heic','wb').write(out)
print(len(out))
