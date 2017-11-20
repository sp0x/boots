<?php

use \Peeralytics\Client;
require __DIR__ . '/vendor/autoload.php';

$appId = "5f059dbe7c3b48d6b3aed518b54f1dc5";
$secret = "BvIn6qz2sOKrgjPBP3PCfSVXESb0Hhn7IhZBKqhObVA=";

$client = new Peeralytics\Client($appId, $secret);
$status = $client->get("data/GetStatus");
var_dump($status);
$newUser = [
	'Name' => "Pesho",
	"Id" => 1
];

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
