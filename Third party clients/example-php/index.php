<?php

use \Netlyt\Client;
require __DIR__ . '/vendor/autoload.php';

$appId = "5f059dbe7c3b48d6b3aed518b54f1dc5";
$secret = "BvIn6qz2sOKrgjPBP3PCfSVXESb0Hhn7IhZBKqhObVA=";

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
$status = $dataClient->createEntity($newUser);
$newUser["Name"] = "Divan";
$newUser["Id"] = 3;
$status = $dataClient->createEntity($newUser);
var_dump($status);

$status = $dataClient->addEntityData([ "Id" => $newUser['Id']], [
	'Action' => 'SiteVisit'
]);

var_dump($status);
die(1);
