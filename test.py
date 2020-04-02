import pandas as pd
import re
import matplotlib.pyplot as plt


f=open("TEST2" + ".txt", "r")
lines = f.readlines()
f.close()

_time = []
_uwb_l = []
_uwb_r = []

# data parse : list in time // cop // imu data :  parse acc.x,y,z : gyro.x,y,z : mag.x,y,z
for line in lines :
    time = line.split(',')
    _time.append(time[0])
    _uwb_l.append(time[11])
    _uwb_r.append(time[13])


# data received and graph


plt.plot(_time,_uwb_r,'-r')
plt.plot(_time,_uwb_l,'-b')

plt.xlabel('time')
plt.ylabel('uwb')
plt.show()
