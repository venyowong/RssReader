# RssReader
Rss Reader in UWP

该项目的 UWP 客户端提交过一次 Windows Store，但是无奈因为完善不佳没通过审核，因此只能将安装包放在 Release 中。另，服务端可正常使用。

## 客户端安装
下载解压 Release 包，右键 Install.ps1，使用 Powershell 运行

## 服务端请求须知
url 根路径：https://venyo.cn/rss/

请求所有接口都需要在 Request Headers 加上一个 appId，此参数可为任何长度五十以内的非空字符串，用于识别发起应用。

### 订阅 rss feed
POST rss/add?feed={feed url}

### 获取已订阅 rss 列表
GET rss/feeds

### 获取文章列表
GET rss/articles?feedId={feedId}&page=0&pageCount=30&endTime{最后的更新时间，可为空}
