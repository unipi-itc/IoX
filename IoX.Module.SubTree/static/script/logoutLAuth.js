function logoutLAuth() {	
	var url = "/modules/lauth/logout";
	$.get( //change to post for passing username 
		url, 
		null, //username
		function(data) {
			console.log( "[logout()] GET data." );
			//control on username logged on
			document.location='loggedOutLAuth.html';
		},
		"json"
	)
	.fail(function() {
		console.log( "[logout()] Error receiving GET data." );
	});
}