# クライアントを作成

import datetime
import socket
import json

with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
    # サーバを指定
    s.connect(('127.0.0.1', 60000))
    # サーバにメッセージを送る
#    data = '\{\"Message\":\"hello\", \"TimeStamp\":\"{0}\"\}'.format(datetime.datetime.now().strftime('%Y/%m/%d %H:%M:%S'))
    data = '{"Message":"hello"}'
    s.sendall(data.encode())
    s.close()
