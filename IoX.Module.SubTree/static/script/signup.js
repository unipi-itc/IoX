var validateTimer;

function signup() {	
	var formData = $("#signupForm").serialize();
	var url = "signup";
	rescheduleValidate();
	$.post( 
		url, 
		formData,
		function(data) {
			if (showValidationResult(data, true))
				document.location='loggedOnLAuth.html';
		},
		"json"
	)
	.fail(function() {
		console.log( "[signup()] ERROR!" );
		document.location='/loginError.html';
	});
}

function rescheduleValidate(timeout) {
	if (validateTimer) {
		clearTimeout(validateTimer);
		validateTimer = undefined;
	}
	if (timeout) {
		validateTimer = setTimeout(validate, timeout);
	}
}

function validate () {	
	var formData = $("#signupForm").serialize();
	var url = "signup-validation";
	rescheduleValidate();
	$.post( 
		url, 
		formData,
		function(data) {
			showValidationResult(data, false);
		},
		"json"
	)
	.fail(function() {
		 console.log( "[validate()] Error receiving POST data." );
	});
}

function showValidationResult (data, allFields) {
	console.log( data );
	var res = true;
	for (var elem in data) 
	{
		if(allFields || $("#"+elem).val() != "")
			res = res && validateText(elem, data[elem]);
	}
	return res;
}

function validateText(id, msg){
	var helpDiv = $(".help-"+id);
	var fieldDiv = $("#"+id).closest("div");
	if(msg == null || msg != "") {
		helpDiv.empty();
		fieldDiv.removeClass("has-success");
		$("#glypcn"+id).remove();
		fieldDiv.addClass("has-error has-feedback");
		fieldDiv.append('<span id="glypcn'+id+'" class="glyphicon glyphicon-remove form-control-feedback"></span>');
		helpDiv.append(msg);
		return false;
	}
	else {
		fieldDiv.removeClass("has-error");
		fieldDiv.addClass("has-success has-feedback");
		$("#glypcn"+id).remove();
		fieldDiv.append('<span id="glypcn'+id+'" class="glyphicon glyphicon-ok form-control-feedback"></span>');
		fieldDiv.append('<div class="help-block" style="display:none;" for="'+id+'">'+$("#"+id).val()+'</div>');
		$(".help-"+id).empty();
		return true;
	}
}



