 
SET FOREIGN_KEY_CHECKS = 0; 
 
-- MySQL dump 10.11
--
-- Host: localhost    Database: SakilaService
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
  `CustomerId` int(11) unsigned NOT NULL,
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
  KEY `FK_CreditCardInfo_Customer` (`CustomerId`),
  CONSTRAINT `FK_CreditCardInfo_Customer` FOREIGN KEY (`CustomerId`) REFERENCES `Customer` (`Id`)
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
  `CustomerProductId` int(10) unsigned NOT NULL,
  `Name` varchar(250) NOT NULL,
  PRIMARY KEY  (`Id`),
  KEY `FK_Genre_CustomerProduct` (`CustomerProductId`),
  CONSTRAINT `FK_Genre_CustomerProduct` FOREIGN KEY (`CustomerProductId`) REFERENCES `CustomerProduct` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
SET character_set_client = @saved_cs_client;

--
-- Table structure for table `Customer`
--

DROP TABLE IF EXISTS `Customer`;
SET @saved_cs_client     = @@character_set_client;
SET character_set_client = utf8;
CREATE TABLE `Customer` (
  `Id` int(11) unsigned NOT NULL auto_increment,
  `GlobalId` binary(16) NOT NULL,
  `CreatedDate` datetime NOT NULL,
  `CreatedBy` varchar(250) NOT NULL,
  `LastModifiedDate` datetime NOT NULL,
  `LastModifiedBy` varchar(250) NOT NULL,
  `CustomerName` varchar(250) NOT NULL,
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
  KEY `FK_Customer_Discussion` (`DiscussionId`),
  CONSTRAINT `FK_Customer_Discussion` FOREIGN KEY (`DiscussionId`) REFERENCES `Discussion` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
SET character_set_client = @saved_cs_client;

--
-- Table structure for table `CustomerOrder`
--

DROP TABLE IF EXISTS `CustomerOrder`;
SET @saved_cs_client     = @@character_set_client;
SET character_set_client = utf8;
CREATE TABLE `CustomerOrder` (
  `Id` int(11) unsigned NOT NULL auto_increment,
  `CustomerId` int(10) unsigned NOT NULL,
  `CustomerGlobalId` binary(16) NOT NULL,
  `CreatedDate` datetime NOT NULL,
  `CreatedBy` varchar(250) NOT NULL,
  `CardName` varchar(250) NOT NULL,
  `CardNo` char(16) NOT NULL,
  `CardCvv` varchar(4) NOT NULL,
  `CardExpiryDate` datetime NOT NULL,
  `Total` decimal(12,2) NOT NULL,
  PRIMARY KEY  (`Id`),
  KEY `FK_CustomerOrder_Customer` (`CustomerId`),
  CONSTRAINT `FK_CustomerOrder_Customer` FOREIGN KEY (`CustomerId`) REFERENCES `Customer` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
SET character_set_client = @saved_cs_client;

--
-- Table structure for table `CustomerOrderLineItem`
--

DROP TABLE IF EXISTS `CustomerOrderLineItem`;
SET @saved_cs_client     = @@character_set_client;
SET character_set_client = utf8;
CREATE TABLE `CustomerOrderLineItem` (
  `Id` int(11) unsigned NOT NULL auto_increment,
  `CustomerOrderId` int(11) unsigned NOT NULL,
  `Name` varchar(250) NOT NULL,
  `UnitPrice` decimal(12,2) NOT NULL,
  `Quantity` int(11) NOT NULL,
  `SubTotal` decimal(12,2) NOT NULL,
  `Vat` decimal(12,2) NOT NULL,
  `Total` decimal(12,2) NOT NULL,
  PRIMARY KEY  (`Id`),
  KEY `FK_CustomerOrderLineItem_CustomerOrder` (`CustomerOrderId`),
  CONSTRAINT `FK_CustomerOrderLineItem_CustomerOrder` FOREIGN KEY (`CustomerOrderId`) REFERENCES `CustomerOrder` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
SET character_set_client = @saved_cs_client;

--
-- Table structure for table `CustomerProduct`
--

DROP TABLE IF EXISTS `CustomerProduct`;
SET @saved_cs_client     = @@character_set_client;
SET character_set_client = utf8;
CREATE TABLE `CustomerProduct` (
  `Id` int(11) unsigned NOT NULL auto_increment,
  `CreatedDate` datetime NOT NULL,
  `CreatedBy` varchar(250) NOT NULL,
  `LastModifiedDate` datetime NOT NULL,
  `LastModifiedBy` varchar(250) NOT NULL,
  `CustomerId` int(11) unsigned NOT NULL,
  `ProductId` int(10) unsigned NOT NULL,
  `AssetId` int(10) unsigned NOT NULL,
  `ParentId` int(11) unsigned default NULL,
  `CustomerOrderId` int(11) unsigned default NULL,
  `PurchaseDate` datetime default NULL,
  `DownloadStartDate` datetime default NULL,
  `DownloadCompleteDate` datetime default NULL,
  PRIMARY KEY  (`Id`),
  KEY `FK_CustomerProduct_Customer` (`CustomerId`),
  KEY `FK_CustomerProduct_CustomerOrder` (`CustomerOrderId`),
  CONSTRAINT `FK_CustomerProduct_Customer` FOREIGN KEY (`CustomerId`) REFERENCES `Customer` (`Id`),
  CONSTRAINT `FK_CustomerProduct_CustomerOrder` FOREIGN KEY (`CustomerOrderId`) REFERENCES `CustomerOrder` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
SET character_set_client = @saved_cs_client;

--
-- Table structure for table `CustomerSet`
--

DROP TABLE IF EXISTS `CustomerSet`;
SET @saved_cs_client     = @@character_set_client;
SET character_set_client = utf8;
CREATE TABLE `CustomerSet` (
  `Id` int(11) unsigned NOT NULL auto_increment,
  `CreatedDate` datetime NOT NULL,
  `CreatedBy` varchar(250) NOT NULL,
  `LastModifiedDate` datetime NOT NULL,
  `LastModifiedBy` varchar(250) NOT NULL,
  `CustomerId` int(11) unsigned NOT NULL,
  `Name` varchar(250) NOT NULL,
  `Type` varchar(250) default NULL,
  `DiscussionId` int(11) unsigned default NULL,
  PRIMARY KEY  (`Id`),
  KEY `FK_CustomerSet_Discussion` (`DiscussionId`),
  KEY `FK_CustomerSet_Customer` (`CustomerId`),
  CONSTRAINT `FK_CustomerSet_Discussion` FOREIGN KEY (`DiscussionId`) REFERENCES `Discussion` (`Id`),
  CONSTRAINT `FK_CustomerSet_Customer` FOREIGN KEY (`CustomerId`) REFERENCES `Customer` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
SET character_set_client = @saved_cs_client;

--
-- Table structure for table `CustomerSetProduct`
--

DROP TABLE IF EXISTS `CustomerSetProduct`;
SET @saved_cs_client     = @@character_set_client;
SET character_set_client = utf8;
CREATE TABLE `CustomerSetProduct` (
  `Id` int(11) unsigned NOT NULL auto_increment,
  `CustomerSetId` int(11) unsigned NOT NULL,
  `ProductId` int(11) unsigned NOT NULL,
  `SortOrder` int(11) default NULL,
  PRIMARY KEY  (`Id`),
  UNIQUE KEY `UNIQUE_CustomerSetId_ProductId` USING BTREE (`CustomerSetId`,`ProductId`),
  CONSTRAINT `FK_CustomerSetProduct_CustomerSet` FOREIGN KEY (`CustomerSetId`) REFERENCES `CustomerSet` (`Id`)
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
