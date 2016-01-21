var iox = (function () {
    var ret = {};
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
        EachList('/status/available-modules', loaded, f, after);
    }
    function ListLoadedModules(loaded, f, after) {
        EachList('/status/modules', loaded, f, after);
    }
    return {
        each_list: EachList,
        available_modules: ListAllModules,
        loaded_modules: ListLoadedModules
    };
})();
