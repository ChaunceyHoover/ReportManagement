CREATE TABLE IF NOT EXISTS `version` (
	`VERSION` INT NOT NULL,
    PRIMARY KEY (`VERSION`))
COMMENT = 'MySQL Versioning System';

DELIMITER $$
DROP PROCEDURE IF EXISTS check_database_version $$
CREATE PROCEDURE check_database_version()

BEGIN

SET @DB_VERSION = (SELECT * FROM `version` LIMIT 1);

IF @DB_VERSION = 1 THEN
# Version 1.0.0, 1.1.0, and 1.2.0
	ALTER TABLE `user` 
	ADD COLUMN `percentage` INT(11) NULL AFTER `is_distributor`;
	UPDATE `version` SET `VERSION` = 2 WHERE `VERSION` = 1;
	
ELSEIF @DB_VERSION = 2 THEN
# Version 1.3.0, 1.4.0
	CREATE TABLE `prizes` (
	  `id` INT NOT NULL AUTO_INCREMENT,
	  `card_id` INT NOT NULL,
	  `cents_won` INT NOT NULL,
	  `site_id` INT NOT NULL,
	  `event_time` DATETIME NOT NULL,
	  PRIMARY KEY (`id`),
	  UNIQUE INDEX `event_time_UNIQUE` (`event_time` ASC),
	  UNIQUE INDEX `card_id_UNIQUE` (`card_id` ASC),
	  UNIQUE INDEX `cents_won_UNIQUE` (`cents_won` ASC))
	COMMENT = 'Stores grand and community prizes';
	
	ALTER TABLE `sites` 
	ADD COLUMN `grand_prize` INT NULL AFTER `comment`,
	ADD COLUMN `community_prize` INT NULL AFTER `grand_prize`,
	ADD COLUMN `lucky_winner_prize` INT NULL AFTER `community_prize`,
	ADD COLUMN `winner_prize` INT NULL AFTER `lucky_winner_prize`,
	ADD COLUMN `store_open_time` TIME NULL DEFAULT '08:00:00' AFTER `winner_prize`,
	ADD COLUMN `store_close_time` TIME NULL DEFAULT '20:00:00' AFTER `store_open_time`,
	ADD COLUMN `twenty_four_seven` TINYINT(1) NULL DEFAULT NULL AFTER `store_close_time`;
	
	UPDATE `version` SET `VERSION` = 3 WHERE `VERSION` = 2;
ELSEIF @DB_VERSION = 3 THEN
# Version 1.5.0
	ALTER TABLE `sites` 
	ADD COLUMN `active_terminals` INT NULL AFTER `twenty_four_seven`,
	ADD COLUMN `inactive_terminals` INT NULL AFTER `active_terminals`;
	
	UPDATE `version` SET `VERSION` = 4 WHERE `VERSION` = 3;
	SELECT 'Successfully ran update, run query again to complete update';
ELSEIF @DB_VERSION = 4 THEN
# Version 1.6.0
	ALTER TABLE `adjustments` 
	  DROP COLUMN `claimed_by_id`,
	  DROP COLUMN `claimed_by_name`,
	  DROP COLUMN `claimed_date`,
	  ADD COLUMN `amount` DECIMAL(11,2) NULL AFTER `month_money_out`,
	  ADD COLUMN `drop_grand_prize` BIT AFTER `month_money_out`,
	  ADD COLUMN `drop_community_prize` BIT AFTER `drop_grand_prize`,
	  ADD COLUMN `card_number` VARCHAR(64) NULL AFTER `adjustment_type`,
	  CHANGE COLUMN `week_money_in` `week_money_in` DECIMAL(11,2) NULL ,
	  CHANGE COLUMN `week_money_out` `week_money_out` DECIMAL(11,2) NULL ,
	  CHANGE COLUMN `month_money_in` `month_money_in` DECIMAL(11,2) NULL ,
	  CHANGE COLUMN `month_money_out` `month_money_out` DECIMAL(11,2) NULL ,
	  CHANGE COLUMN `money_money_out` `month_money_out` DECIMAL(11,2) NOT NULL,
	  CHANGE COLUMN `adjustment_ip` `adjustment_ip` VARCHAR(255) CHARACTER SET 'utf8' NULL ,
	  CHANGE COLUMN `time_ran` `time_ran` DATETIME NOT NULL DEFAULT '1970-01-01 00:00:00' ,
	  CHANGE COLUMN `restart_time` `restart_time` DATETIME NOT NULL DEFAULT '1970-01-01 00:00:00' ,
	  CHANGE COLUMN `reset_request` `reset_request` BIT(1) NOT NULL DEFAULT b'0' ,
	  CHANGE COLUMN `secret_key` `secret_key` VARCHAR(36) CHARACTER SET 'utf8' NULL,
	  CHANGE COLUMN `completed_date` `completed_date` DATETIME NOT NULL DEFAULT '1970-01-01 00:00:00';
	
	CREATE TABLE `stk`.`players` (
	  `site_id` INT NOT NULL,
	  `card_id` INT NOT NULL,
	  `first_name` VARCHAR(45) NULL,
	  `last_name` VARCHAR(45) NULL,
	  `city` VARCHAR(45) NULL,
	  `state` VARCHAR(45) NULL,
	  `zip` VARCHAR(45) NULL,
	  `phone` VARCHAR(45) NULL,
	  `birthdate` DATETIME NULL DEFAULT '1970-01-01 00:00:00',
	  `gender` BIT NULL,
	  `personal_id` VARCHAR(45) NULL,
	  `data_enter_type_id` INT NULL,
	  `notes` VARCHAR(45) NULL,
	  `email_address` VARCHAR(45) NULL,
	  PRIMARY KEY (`site_id`, `card_id`));

	
	UPDATE `version` SET `VERSION` = 5 WHERE `VERSION` = 4;
	SELECT 'Successfully ran update, run query again to complete update';
ELSEIF @DB_VERSION = 5 THEN
	CREATE TABLE `adjustment_types` (
	  `adjustment_type` INT NOT NULL,
	  `adjustment_name` NVARCHAR(128) NOT NULL,
	  `adjustment_query` NVARCHAR(2048) NOT NULL,
	  PRIMARY KEY (`adjustment_type`));
	
	INSERT INTO `adjustment_types` VALUES
	  (0, 'Small Increase', 'UPDATE [dbo].[PrizeTable] SET iSweepstakesFrequency = iSweepstakesFrequency + (iSweepstakesFrequency  * .05) WHERE iGameDefID != 2000 AND iLinesPlayed > 1 AND iCreditsWagered > 0 AND iCreditsWon > 30'),
	  (1, 'Medium Increase', 'UPDATE [dbo].[PrizeTable] SET iSweepstakesFrequency = iSweepstakesFrequency + (iSweepstakesFrequency  * .1) WHERE iGameDefID != 2000 AND iLinesPlayed > 1 AND iCreditsWagered > 0 AND iCreditsWon > 30'),
	  (2, 'Large Increase', 'UPDATE [dbo].[PrizeTable] SET iSweepstakesFrequency = iSweepstakesFrequency + (iSweepstakesFrequency  * .2) WHERE iGameDefID != 2000 AND iLinesPlayed > 1 AND iCreditsWagered > 0 AND iCreditsWon > 30'),
	  (3, 'Small Decrease', 'UPDATE [dbo].[PrizeTable] SET iSweepstakesFrequency = iSweepstakesFrequency - (iSweepstakesFrequency  * .05) WHERE iGameDefID != 2000 AND iLinesPlayed > 1 AND iCreditsWagered > 0 AND iCreditsWon > 30'),
	  (4, 'Medium Decrease', 'UPDATE [dbo].[PrizeTable] SET iSweepstakesFrequency = iSweepstakesFrequency - (iSweepstakesFrequency  * .1) WHERE iGameDefID != 2000 AND iLinesPlayed > 1 AND iCreditsWagered > 0 AND iCreditsWon > 30'),
	  (5, 'Large Decrease', 'UPDATE [dbo].[PrizeTable] SET iSweepstakesFrequency = iSweepstakesFrequency - (iSweepstakesFrequency  * .2) WHERE iGameDefID != 2000 AND iLinesPlayed > 1 AND iCreditsWagered > 0 AND iCreditsWon > 30'),
	  (6, 'Just A Reset', ''),
	  (7, 'Drop Grand Prize', 'UPDATE [PrizeTable] SET iPattern = 0, iSweepstakesFrequency = 9999999 WHERE iGameDefID = 2000 AND (iPattern = 100 OR iPattern = 0)'),
	  (8, 'Drop Community Prize', 'UPDATE [PrizeTable] SET iPattern = 1, iSweepstakesFrequency = 9999999 WHERE iGameDefID = 2000 AND (iPattern = 100 OR iPattern = 0)'),
	  (9, 'Playable', "UPDATE [dbo].[PlayerTracking_PlayerCard] SET strNonCashableBalance = N'{0}' WHERE iCardID = {1}"),
	  (10, 'Cashable', "UPDATE [dbo].[PlayerTracking_PlayerCard] SET strCashableBalance = N'{0}' WHERE iCardID = {1})");

	CREATE TABLE `community_table` (
	  `card_id` INT NOT NULL,
	  `site_id` INT NOT NULL,
	  `event_time` TIMESTAMP NOT NULL,
	  `cents_won` INT NOT NULL,
	  PRIMARY KEY (`card_id`));

	ALTER TABLE `sites` 
	  ADD COLUMN `last_player_update` DATETIME NOT NULL DEFAULT '1970-01-01 00:00:00' AFTER `inactive_terminals`,
	  ADD COLUMN `last_community_update` DATETIME NOT NULL DEFAULT '1970-01-01 00:00:00' AFTER `last_player_update`,
	  ADD COLUMN `last_community_drop` DATETIME NOT NULL DEFAULT '1970-01-01 00:00:00' AFTER `last_community_update`,
	  ADD COLUMN `last_grand_drop` DATETIME NOT NULL DEFAULT '1970-01-01 00:00:00' AFTER `last_community_drop`,
	  CHANGE COLUMN `drop_grand_prize` `drop_grand_prize` TINYINT(1) NULL DEFAULT 0 ,
	  CHANGE COLUMN `drop_community_prize` `drop_community_prize` TINYINT(1) NULL DEFAULT 0,
	  CHANGE COLUMN `completed` `completed` TINYINT(1) NULL DEFAULT 0,
	  CHANGE COLUMN `reset_request` `reset_request` TINYINT(1) NULL DEFAULT 0;


	UPDATE `version` SET `VERSION` = 6 WHERE `VERSION` = 5;
	SELECT 'Database successfully updated to latest version';
ELSE
	SELECT 'Unknown Version Number (did someone mess with the MySQL database?)';
    INSERT INTO `version` VALUES (1);
END IF;

END $$

CALL check_database_version() $$

DROP PROCEDURE IF EXISTS check_database_version $$

DELIMITER ;