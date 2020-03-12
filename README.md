# 图片上位机
## 0、开发思路
C#开发的串口助手程序,可以将单片机采集摄像头RGB565格式数据直接显示图片
## 1、版本
**V1.0**
单片机采集摄像头数据为RGB565格式，该上位机利用串口接收单片机数据，解码为RGB555数据，在上位机显示bmp图像，并可以保存图片。

**V1.1**
解决只能接收一次数据问题，
解决图片偏黑问题，
提高发送波特率，
修改一些BUG。

**V2.0**
增加网络调试助手，通过WiFi接收数据，速度更快。

## 2、效果图片
![在这里插入图片描述](https://img-blog.csdnimg.cn/20200312091116288.png?x-oss-process=image/watermark,type_ZmFuZ3poZW5naGVpdGk,shadow_10,text_aHR0cHM6Ly9ibG9nLmNzZG4ubmV0L3FxXzM3ODMyOTMy,size_16,color_FFFFFF,t_70)
## 3、关键代码
RGB565转换为RGB555
```
r = (colorH & 0xf8) >> 3;
g = ((colorH & 0x07) << 2) | ((colorL & 0xe0) >> 6);
b = colorL & 0x1f;
```
## 4、More
CSDN：[https://blog.csdn.net/qq_37832932](https://blog.csdn.net/qq_37832932)
