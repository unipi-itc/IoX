function logoutOAuth() {	
	var url = "/modules/oauth/logout";
	$.get( //change to post for passing username 
		url, 
		null, //username
		function(data) {
			console.log( "[logout()] GET data." );
			//control on username logged on
			document.location='loggedOutOAuth.html';
		},
		"json"
	)
	.fail(function() {
		console.log( "[logout()] Error receiving GET data." );
	});
}