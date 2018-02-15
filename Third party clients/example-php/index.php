<?php

use \Netlyt\Client;
require __DIR__ . '/vendor/autoload.php';

$appId = "d4af4a7e3b1346e5a406123782799da1";
$secret = "cwbdGy4mqK9n10OJDPWpvbHuR5jUsvVDY5b1TfdNRmE=";

$client = new Netlyt\Client($appId, $secret);
//Get the status of our client
$status = $client->get("data/GetStatus");
$newUser = [
	'Name' => "Peshovec",
	"Id" => 12
];
//Creating a data client, to work with user data
$dataClient = $client->getDataClient();
//create an user entity
$status = $dataClient->createEntity($newUser);

//Add event data to this entity
$status = $dataClient->addEntityData([ "Id" => $newUser['Id']], [
	'Action' => 'SiteVisit'
]);

var_dump($status);

$permissionsArray = ["email"];
$client->requirePermissions("Facebook", $permissionsArray);
var_dump($permissionsArray);

$dataClient = $client->getDataClient(); 
$newUser["Name"] = "Divan";
$newUser["Id"] = 3;
$status = $dataClient->createEntity($newUser);
var_dump($status);

$status = $dataClient->addEntityData([ "Id" => $newUser['Id']], [
	'Action' => 'SiteVisit'
]);

var_dump($status);
die(1);
