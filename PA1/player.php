<?php

class Player {

    private $name;
    private $stats;

    public function Player($name, $stats) {
        $this->name = $name;
        $this->stats = $stats;
    }

    public function getName() {
        return $this->name;
    }

    public function getStats() {
        return $this->stats;
    }
    
}

?>