 
SET FOREIGN_KEY_CHECKS = 0; 
 
-- MySQL dump 10.11
--
-- Host: localhost    Database: @ServiceName@
-- ------------------------------------------------------
-- Server version	5.0.67-community-nt

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `CreditCardInfo`
--

DROP TABLE IF EXISTS `CreditCardInfo`;
SET @saved_cs_client     = @@character_set_client;
SET character_set_client = utf8;
CREATE TABLE `CreditCardInfo` (
  `Id` int(11) unsigned NOT NULL auto_increment,
  `@ModelName@Id` int(11) unsigned NOT NULL,
  `IsActive` tinyint(1) unsigned NOT NULL default '0',
  `CardType` varchar(250) NOT NULL,
  `CardHolderName` varchar(250) NOT NULL,
  `CardNumber` char(16) NOT NULL,
  `CardCvv` varchar(4) NOT NULL,
  `CardExpiryDate` datetime NOT NULL,
  `BillingAddressLine1` varchar(250) NULL,
  `BillingAddressLine2` varchar(250) NULL,
  `BillingAddressTown` varchar(250)  NULL,
  `BillingAddressCounty` varchar(250)  NULL,
  `BillingAddressPostCode` varchar(250) NULL,
  
  PRIMARY KEY  (`Id`),
  KEY `FK_CreditCardInfo_@ModelName@` (`@ModelName@Id`),
  CONSTRAINT `FK_CreditCardInfo_@ModelName@` FOREIGN KEY (`@ModelName@Id`) REFERENCES `@ModelName@` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
SET character_set_client = @saved_cs_client;

--
-- Table structure for table `Discussion`
--

DROP TABLE IF EXISTS `Discussion`;
SET @saved_cs_client     = @@character_set_client;
SET character_set_client = utf8;
CREATE TABLE `Discussion` (
  `Id` int(11) unsigned NOT NULL auto_increment,
  `CreatedDate` datetime NOT NULL,
  PRIMARY KEY  (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
SET character_set_client = @saved_cs_client;

--
-- Table structure for table `DiscussionPage`
--

DROP TABLE IF EXISTS `DiscussionPage`;
SET @saved_cs_client     = @@character_set_client;
SET character_set_client = utf8;
CREATE TABLE `DiscussionPage` (
  `Id` int(11) unsigned NOT NULL auto_increment,
  `DiscussionId` int(11) unsigned NOT NULL,
  `CreatedDate` datetime NOT NULL,
  `LastModifiedDate` datetime NOT NULL,
  `PostCount` int(11) NOT NULL,
  `PostContent` text NOT NULL,
  `PostStatistics` blob,
  PRIMARY KEY  (`Id`),
  KEY `FK_DiscussionPage_Discussion` (`DiscussionId`),
  CONSTRAINT `FK_DiscussionPage_Discussion` FOREIGN KEY (`DiscussionId`) REFERENCES `Discussion` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
SET character_set_client = @saved_cs_client;

--
-- Table structure for table `Genre`
--

DROP TABLE IF EXISTS `Genre`;
SET @saved_cs_client     = @@character_set_client;
SET character_set_client = utf8;
CREATE TABLE `Genre` (
  `Id` int(10) unsigned NOT NULL auto_increment,
  `@ModelName@ProductId` int(10) unsigned NOT NULL,
  `Name` varchar(250) NOT NULL,
  PRIMARY KEY  (`Id`),
  KEY `FK_Genre_@ModelName@Product` (`@ModelName@ProductId`),
  CONSTRAINT `FK_Genre_@ModelName@Product` FOREIGN KEY (`@ModelName@ProductId`) REFERENCES `@ModelName@Product` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
SET character_set_client = @saved_cs_client;

--
-- Table structure for table `@ModelName@`
--

DROP TABLE IF EXISTS `@ModelName@`;
SET @saved_cs_client     = @@character_set_client;
SET character_set_client = utf8;
CREATE TABLE `@ModelName@` (
  `Id` int(11) unsigned NOT NULL auto_increment,
  `GlobalId` binary(16) NOT NULL,
  `CreatedDate` datetime NOT NULL,
  `CreatedBy` varchar(250) NOT NULL,
  `LastModifiedDate` datetime NOT NULL,
  `LastModifiedBy` varchar(250) NOT NULL,
  `@ModelName@Name` varchar(250) NOT NULL,
  `Title` varchar(5) NULL,
  `Gender` char(1) NULL,
  `FirstName` varchar(250) NOT NULL,
  `LastName` varchar(250) NOT NULL,
  `SaltPassword` varchar(250) NOT NULL,
  `Balance` decimal(12,2) NOT NULL default '0.00',
  `Email` varchar(250) NOT NULL,
  `Country` varchar(250) NOT NULL,
  `LanguageCode` char(5) NOT NULL,
  `CanNotifyEmail` tinyint(1) unsigned NOT NULL default '0',
  `StoreCreditCard` tinyint(1) unsigned NOT NULL default '0',
  `SingleClickBuyEnabled` tinyint(1) unsigned NOT NULL default '0',
  `DiscussionId` int(11) unsigned default NULL,
  PRIMARY KEY  (`Id`),
  KEY `FK_@ModelName@_Discussion` (`DiscussionId`),
  CONSTRAINT `FK_@ModelName@_Discussion` FOREIGN KEY (`DiscussionId`) REFERENCES `Discussion` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
SET character_set_client = @saved_cs_client;

--
-- Table structure for table `@ModelName@Order`
--

DROP TABLE IF EXISTS `@ModelName@Order`;
SET @saved_cs_client     = @@character_set_client;
SET character_set_client = utf8;
CREATE TABLE `@ModelName@Order` (
  `Id` int(11) unsigned NOT NULL auto_increment,
  `@ModelName@Id` int(10) unsigned NOT NULL,
  `@ModelName@GlobalId` binary(16) NOT NULL,
  `CreatedDate` datetime NOT NULL,
  `CreatedBy` varchar(250) NOT NULL,
  `CardName` varchar(250) NOT NULL,
  `CardNo` char(16) NOT NULL,
  `CardCvv` varchar(4) NOT NULL,
  `CardExpiryDate` datetime NOT NULL,
  `Total` decimal(12,2) NOT NULL,
  PRIMARY KEY  (`Id`),
  KEY `FK_@ModelName@Order_@ModelName@` (`@ModelName@Id`),
  CONSTRAINT `FK_@ModelName@Order_@ModelName@` FOREIGN KEY (`@ModelName@Id`) REFERENCES `@ModelName@` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
SET character_set_client = @saved_cs_client;

--
-- Table structure for table `@ModelName@OrderLineItem`
--

DROP TABLE IF EXISTS `@ModelName@OrderLineItem`;
SET @saved_cs_client     = @@character_set_client;
SET character_set_client = utf8;
CREATE TABLE `@ModelName@OrderLineItem` (
  `Id` int(11) unsigned NOT NULL auto_increment,
  `@ModelName@OrderId` int(11) unsigned NOT NULL,
  `Name` varchar(250) NOT NULL,
  `UnitPrice` decimal(12,2) NOT NULL,
  `Quantity` int(11) NOT NULL,
  `SubTotal` decimal(12,2) NOT NULL,
  `Vat` decimal(12,2) NOT NULL,
  `Total` decimal(12,2) NOT NULL,
  PRIMARY KEY  (`Id`),
  KEY `FK_@ModelName@OrderLineItem_@ModelName@Order` (`@ModelName@OrderId`),
  CONSTRAINT `FK_@ModelName@OrderLineItem_@ModelName@Order` FOREIGN KEY (`@ModelName@OrderId`) REFERENCES `@ModelName@Order` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
SET character_set_client = @saved_cs_client;

--
-- Table structure for table `@ModelName@Product`
--

DROP TABLE IF EXISTS `@ModelName@Product`;
SET @saved_cs_client     = @@character_set_client;
SET character_set_client = utf8;
CREATE TABLE `@ModelName@Product` (
  `Id` int(11) unsigned NOT NULL auto_increment,
  `CreatedDate` datetime NOT NULL,
  `CreatedBy` varchar(250) NOT NULL,
  `LastModifiedDate` datetime NOT NULL,
  `LastModifiedBy` varchar(250) NOT NULL,
  `@ModelName@Id` int(11) unsigned NOT NULL,
  `ProductId` int(10) unsigned NOT NULL,
  `AssetId` int(10) unsigned NOT NULL,
  `ParentId` int(11) unsigned default NULL,
  `@ModelName@OrderId` int(11) unsigned default NULL,
  `PurchaseDate` datetime default NULL,
  `DownloadStartDate` datetime default NULL,
  `DownloadCompleteDate` datetime default NULL,
  PRIMARY KEY  (`Id`),
  KEY `FK_@ModelName@Product_@ModelName@` (`@ModelName@Id`),
  KEY `FK_@ModelName@Product_@ModelName@Order` (`@ModelName@OrderId`),
  CONSTRAINT `FK_@ModelName@Product_@ModelName@` FOREIGN KEY (`@ModelName@Id`) REFERENCES `@ModelName@` (`Id`),
  CONSTRAINT `FK_@ModelName@Product_@ModelName@Order` FOREIGN KEY (`@ModelName@OrderId`) REFERENCES `@ModelName@Order` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
SET character_set_client = @saved_cs_client;

--
-- Table structure for table `@ModelName@Set`
--

DROP TABLE IF EXISTS `@ModelName@Set`;
SET @saved_cs_client     = @@character_set_client;
SET character_set_client = utf8;
CREATE TABLE `@ModelName@Set` (
  `Id` int(11) unsigned NOT NULL auto_increment,
  `CreatedDate` datetime NOT NULL,
  `CreatedBy` varchar(250) NOT NULL,
  `LastModifiedDate` datetime NOT NULL,
  `LastModifiedBy` varchar(250) NOT NULL,
  `@ModelName@Id` int(11) unsigned NOT NULL,
  `Name` varchar(250) NOT NULL,
  `Type` varchar(250) default NULL,
  `DiscussionId` int(11) unsigned default NULL,
  PRIMARY KEY  (`Id`),
  KEY `FK_@ModelName@Set_Discussion` (`DiscussionId`),
  KEY `FK_@ModelName@Set_@ModelName@` (`@ModelName@Id`),
  CONSTRAINT `FK_@ModelName@Set_Discussion` FOREIGN KEY (`DiscussionId`) REFERENCES `Discussion` (`Id`),
  CONSTRAINT `FK_@ModelName@Set_@ModelName@` FOREIGN KEY (`@ModelName@Id`) REFERENCES `@ModelName@` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
SET character_set_client = @saved_cs_client;

--
-- Table structure for table `@ModelName@SetProduct`
--

DROP TABLE IF EXISTS `@ModelName@SetProduct`;
SET @saved_cs_client     = @@character_set_client;
SET character_set_client = utf8;
CREATE TABLE `@ModelName@SetProduct` (
  `Id` int(11) unsigned NOT NULL auto_increment,
  `@ModelName@SetId` int(11) unsigned NOT NULL,
  `ProductId` int(11) unsigned NOT NULL,
  `SortOrder` int(11) default NULL,
  PRIMARY KEY  (`Id`),
  UNIQUE KEY `UNIQUE_@ModelName@SetId_ProductId` USING BTREE (`@ModelName@SetId`,`ProductId`),
  CONSTRAINT `FK_@ModelName@SetProduct_@ModelName@Set` FOREIGN KEY (`@ModelName@SetId`) REFERENCES `@ModelName@Set` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
SET character_set_client = @saved_cs_client;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2008-12-10 10:30:25
 
SET FOREIGN_KEY_CHECKS = 1; 
