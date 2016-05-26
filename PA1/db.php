<?php

class Database {

    public $host = 'nnpa1.cdnehwffc0c2.us-west-2.rds.amazonaws.com:3306';
    public $user = 'info344user';
    public $pass = 'nbapassword';
    public $dbname = 'NBAPLAYERS';

    public $dbh;
    public $error;

    public $stmt;

    public function connect(){
        $dsn = 'mysql:host=' . $this->host . ';dbname=' . $this->dbname;
        $options = array(
            //PDO::ATTR_PERSISTENT    => true,
            PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION
        );
        try{
            $this->dbh = new PDO($dsn, $this->user, $this->pass, $options);
        }
        catch(PDOException $e){
            $this->error = $e->getMessage();
        }
    }

    public function prepare($query){
        $this->stmt = $this->dbh->prepare($query);
    }

    public function bind($param, $value){
        $this->stmt->bindValue($param, $value, PDO::PARAM_STR);
    }

    public function query($query){
        $this->stmt = $this->dbh->query($query);
    }

    public function execute(){
        return $this->stmt->execute();
    }

    /*public function resultset(){
        $this->execute();
        return $this->stmt->fetchAll(PDO::FETCH_ASSOC);
    }*/

    public function single(){
        $this->execute();
        return $this->stmt->fetch(PDO::FETCH_ASSOC);
    }

    //$database->prepare('SELECT FName, LName, Age, Gender FROM mytable WHERE FName = :fname');
    //$database->bind(':fname', 'Jenny');
    //$row = $database->single();
}


?>