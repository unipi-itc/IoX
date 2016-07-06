var iox = (function () {
  var ret = {};
  function PostJSON(url, data, success) {
    $.ajax({
      url: url,
      type: 'POST',
      data: JSON.stringify(data),
      contentType: 'application/json',
      dataType: "json",
      success: success
    });
  }
  function EachList(url, loaded, f, after) {
    $.getJSON(url, function (data) {
      loaded();
      $.each(data, function (k, m) {
        f(m);
      });
      if (after)
        after();
    });
  }
  function ListAllModules(loaded, f, after) {
    EachList('available-modules', loaded, f, after);
  }
  function UpdateModulesList(relPath) {
    $("#ModulesDropDown .dynamic-menu-item").remove();
    var divider = $("#ModulesDropDown .divider");
    divider.before($('<li><a class="dynamic-menu-item" href="#">Loading...</a></li>'));
    EachList(
      relPath + 'loaded-modules',
      function () {
        $("#ModulesDropDown .dynamic-menu-item").remove();
      },
      function (m) {
        divider.before($('<li><a class="dynamic-menu-item" href="' + m.Url + '">' + m.Name + '</a></li>'));
      }
    );
  }
  return {
    post_json: PostJSON,
    each_list: EachList,
    available_modules: ListAllModules,
    update_modules_list: UpdateModulesList,
  };
})();
