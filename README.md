I am using [inoreader](https://www.inoreader.com/) as my RSS tool.

# RssReader
Rss Reader in UWP

## Install Client
Download and unzip [Resader release package](https://github.com/venyowong/RssReader/releases), then right click `Install.ps1`, and use `Powershell` run it

## RssServer Tips
Base url: https://venyo.cn/rss/

All interfaces need to add an `appid` parameter to the request headers. This parameter can be any non empty string with a length of less than 50 to identify the originating application.

### Subscribe rss feed
POST rss/add?feed={feed url}

### Get list of subscribed rss
GET rss/feeds

### Get article list
GET rss/articles?feedId={feedId}&page=0&pageCount=30&endTime={Last update time, allow empty}

### Delete subscription
DELETE rss/feed?feedId={feedId}

### Batch subscribe
POST rss/addfeeds

Request Body:
```
[
    {feed url},
    {feed url}
]
```


该项目的 UWP 客户端提交过一次 Windows Store，但是无奈因为完善不佳没通过审核，因此只能将安装包放在 Release 中。另，服务端可正常使用。

## 客户端安装
下载解压 [Resader release 包](https://github.com/venyowong/RssReader/releases)，右键 `Install.ps1`，使用 `Powershell` 运行

## 服务端请求须知
url 根路径：https://venyo.cn/rss/

请求所有接口都需要在 Request Headers 加上一个 `appId`，此参数可为任何长度五十以内的非空字符串，用于识别发起应用。

### 订阅 rss feed
POST rss/add?feed={feed url}

### 获取已订阅 rss 列表
GET rss/feeds

### 获取文章列表
GET rss/articles?feedId={feedId}&page=0&pageCount=30&endTime={最后的更新时间，可为空}

### 删除订阅
DELETE rss/feed?feedId={feedId}

### 批量订阅
POST rss/addfeeds

Request Body:
```
[
    {feed url},
    {feed url}
]
```
