app.vm = (function () {
    //"use strict";
    var model = new app.DashboardDisplays();

    // #region CONTROLS                
    var forRestructure = ko.observable();
    // #endregion

    // #region BEHAVIORS
    // initializers
    function activate() {
        update();
    }

    function update() {
        $.ajax({
            url: RootUrl + "Home/LoadDashboard",
            type: "POST",
            dataType: "json",
            contentType: 'application/json; charset=utf-8',
            success: function (result) {
                model.Released(result[0].Released);
                model.Collection(result[0].Collection)
                model.Receivables(result[0].Receivables)

                forRestructure(result[0].ForRestructure)
            }   
        });
    }

    // #endregion

    var vm = {
        activate: activate,
        update: update,
        model: model,
        forRestructure: forRestructure
    };
    return vm;a
})();
$(function () {
    "use strict";

    $.wait = function (callback, seconds) {
        return window.setTimeout(callback, seconds * 1000);
    }

    if (Released != "" && Collection != "" && Receivables != "") {
        app.vm.model.Released(Released);
        app.vm.model.Collection(Collection)
        app.vm.model.Receivables(Receivables)

        app.vm.forRestructure(ForRestructure)

        $.wait(function () { app.vm.update() }, 3000);
    }
    else {
        //app.vm.update();
    }

    //window.setTimeout(app.vm.update(), 5000);

    ko.applyBindings(app.vm);
});