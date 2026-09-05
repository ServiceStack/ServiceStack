USE test;

CREATE TABLE IF NOT EXISTS fts_health (
    id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    content TEXT NOT NULL,
    FULLTEXT KEY ix_fts_health_content (content)
) ENGINE=InnoDB;
