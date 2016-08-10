var loadIoxModule = function(baseUrl, container) {
  var url = baseUrl + "module.js";
  var request = new XMLHttpRequest();
  request.open("GET", url, true);
  request.onload = function() {
    if (request.status >= 200 && request.status < 400) {
      var transpiled = babel.transform(request.responseText);
      var moduleF = eval(transpiled.code);
      moduleF(baseUrl, container);
    } else {
      // TODO
    }
  }
  request.onerror = function() {
    //TODO
  }
  request.send();
}
