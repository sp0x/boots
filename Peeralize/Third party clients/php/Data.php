<?php
namespace Peeralytics;

class Data{
	private $client;
	public function __construct(\Peeralytics\Client $client){
		$this->client = $client;
		$this->client->setPrefix("data");
	}
	
	public function put($data){
		return $this->client->post("ClientData" , $data);
	}
}