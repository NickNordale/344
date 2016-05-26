<?php

//$callback = $_GET['callback'];
//$data = [data stuff that you are passing back];
//echo $callback . "(" . json_encode($data) . ")";

//include 'db.php';
//include 'player.php';

$conn = new PDO('mysql:host=nnpa1.cdnehwffc0c2.us-west-2.rds.amazonaws.com:3306;dbname=NBAPLAYERS', 'info344user', 'nbapassword');
$conn->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);

//$db = new Database();
//$db->connect();

if(isset($_GET['playerName'])) {
	$name = $_GET['playerName'];
} else {
	$name = "";
}

$cols = $conn->query('SELECT * FROM `INFORMATION_SCHEMA`.`COLUMNS` WHERE `TABLE_SCHEMA`=\'NBAPLAYERS\' AND `TABLE_NAME`=\'PLAYERS\'');

$search = $conn->prepare('SELECT * FROM `PLAYERS` WHERE `Player` LIKE :searchName');
$search->execute(array(":searchName" => "%" . $name . "%"));

$results = array();
if ($search->execute()) {
	while ($row = $search->fetch(PDO::FETCH_ASSOC)) {
		$results[] = $row;
	}
}

?>

<!DOCTYPE html>
<html lang="en">
<head>
	<meta charset="UTF-8">
	<meta name="viewport" content="width=device-width, initial-scale=1.0">
	<title>NBA Stats</title>

	<link rel="stylesheet" href="main.css">

	<!-- Latest compiled and minified CSS -->
	<link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/bootstrap/3.3.6/css/bootstrap.min.css" integrity="sha384-1q8mTJOASx8j1Au+a5WDVnPi2lkFfwwEAa8hDDdjZlpLegxhjVME1fgjWPGmkzs7" crossorigin="anonymous">

	<script src="https://code.jquery.com/jquery-1.12.4.min.js" integrity="sha256-ZosEbRLbNQzLpnKIkEdrPv7lOy9C27hHQ+Xp8a4MxAQ=" crossorigin="anonymous"></script>
</head>
<body>

<div class="container">

	<div class="row" id="search-row">
		<a href="http://ec2-52-38-169-3.us-west-2.compute.amazonaws.com/">
			<img id="logo" src="http://www.officialpsds.com/images/thumbs/NBA-logo-psd15019.png" alt="">
		</a>
		<script>
			$( document ).ready(function() {
				$("#playerName").keyup(function () {
					var keyword = $("#playerName").val();
					$.ajax({
						url: '/index.php',
						type: 'GET',
						data: { playerName: keyword },
						success: function (result) {
							console.log(result);
							console.log(keyword);
							var trim1 = $.trim(result.substr(result.indexOf("<tr id=\"res\">")));
							var trim2 = $.trim(trim1.substr(0, trim1.indexOf("</tbody>")));
							$("#table-body").html(trim2);
						}
					});
				});
			});
		</script>
		<form action = "" method = "GET">
			<label for="playerName">Search for players by name:</label>
			<fieldset class="form-inline">
				<input type="text" class="form-control" id="playerName" name="playerName" placeholder="player name">
				<button type="submit" class="btn btn-primary">Go</button>
			</fieldset>
		</form>
	</div>

	<div class="row" id="results-row">
		<table class="table table-hover">
			<thead>
			<tr>
				<?php foreach($cols as $attr): ?>
					<th><?php echo $attr['COLUMN_NAME']; ?></th>
				<?php endforeach; ?>
			</tr>
			</thead>
			<tbody id="table-body">
			<?php foreach($results as $player): ?>
				<tr id="res">
					<?php foreach($player as $stat): ?>
						<td><?php echo $stat; ?></td>
					<?php endforeach; ?>
				</tr>
			<?php endforeach; ?>
			</tbody>
		</table>
	</div>
</div>

</body>
</html>