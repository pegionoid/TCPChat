# クライアントを作成

import sys
import socket
import struct #数値→バイト列変換用
import time
import threading

DISP_ID = 0

# サーバIP
TCP_SND_IP   = '127.0.0.1'
# サーバポート
TCP_SND_PORT = 60000

# 生存監視受信ポート
TCP_RCV_PORT = 60000

# TCPサーバクラス
# 初期化応答の受信、生存監視への返答、終了通知の受信を行う
class TCP_SERVER(threading.Thread):
    # 初期化処理
    def __init__(self, PORT):
        threading.Thread.__init__(self)
        self.kill_flag = False

        self.HOST = socket.gethostname()
        self.PORT = PORT
        self.BUFSIZE = 1024


    # 受信待ち
    def run(self):
        # TCPサーバ開始
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as self.TCP_SOCKET:
             self.TCP_SOCKET.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
             self.TCP_SOCKET.settimeout(60)
             try:
                 self.TCP_SOCKET.bind((TCP_SND_IP, self.PORT))
                 self.TCP_SOCKET.listen(5)
                 print('TCPServerStart')

                 (connection, client) = self.TCP_SOCKET.accept()
                 print('Client connected', client)

                 # 無限ループ
                 while True :
                     
                     data = connection.recv(self.BUFSIZE)
                     print('Received Data : {}'.format(data.decode("utf-8")))

                     connection.send(data);
                     
             except socket.timeout:
                 print('timeout')
                 self.TCP_SOCKET


if __name__ == '__main__':
    ts = TCP_SERVER(TCP_RCV_PORT)
    ts.setDaemon(True)
    ts.start()

    ts.join()
