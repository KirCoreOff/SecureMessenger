CREATE DATABASE `securemessenger` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;

use securemessenger;

CREATE TABLE `users` (
  `login` varchar(255) NOT NULL,
  `password` blob,
  PRIMARY KEY (`login`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `messages` (
  `messageId` int NOT NULL AUTO_INCREMENT,
  `senderLogin` varchar(255) NOT NULL,
  `messageText` blob,
  `FilePath` blob,
  `timestamp` datetime NOT NULL,
  PRIMARY KEY (`messageId`),
  KEY `senderLogin` (`senderLogin`),
  CONSTRAINT `messages_ibfk_1` FOREIGN KEY (`senderLogin`) REFERENCES `users` (`login`)
) ENGINE=InnoDB AUTO_INCREMENT=197 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
