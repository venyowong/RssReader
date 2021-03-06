DROP DATABASE IF EXISTS rssserver;

CREATE DATABASE rssserver;

USE rssserver;

CREATE TABLE feed(
id VARCHAR(50) NOT NULL PRIMARY KEY,
url VARCHAR(200) NOT NULL,
title VARCHAR(200) NOT NULL
);

CREATE TABLE article(
id VARCHAR(100) NOT NULL PRIMARY KEY,
url VARCHAR(200) NOT NULL,
feed_id VARCHAR(50) NOT NULL,
title VARCHAR(200) NOT NULL,
summary VARCHAR(500),
published DATETIME,
updated DATETIME,
created DATETIME NOT NULL,
keyword VARCHAR(300),
content VARCHAR(500),
contributors VARCHAR(100),
authors VARCHAR(100),
copyright VARCHAR(100)
);

CREATE TABLE subscription(
id INT PRIMARY KEY AUTO_INCREMENT,
app_id VARCHAR(50) NOT NULL,
feed_id VARCHAR(50) NOT NULL
);