window.onload = loadProviders;

function loginRedirect(provider) {
	document.location='oaquery?provider='+provider;
}

String.prototype.capitalizeFirstLetter = function() {
    return this.charAt(0).toUpperCase() + this.slice(1);
}

function loadProviders() {
	var url = "load-providers";
	$.get( 
		url, 
		null,
		function(data) {
			for (elem in data) {
				var input = document.createElement("span"); 
				var capLetter = elem.capitalizeFirstLetter();
				input.innerHTML = '<a class="btn btn-block btn-social btn-'+elem+'" onclick="loginRedirect(\''+elem+'\')"><span class="fa fa-'+elem+'"></span> Sign in with '+capLetter+'</a>';	
				document.getElementById("social-buttons").appendChild(input.firstChild );
			}
		},
		"json"
	)
	.fail(function() {
		console.log( "[loadProviders()] Error." );
	});
}
