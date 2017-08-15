app.vm = (function () {
    //"use strict";
    var model = new app.createModel();

    // #region CONTROLS                
    var isListShowed = ko.observable(true);
    var isCreateShowed = ko.observable(false);

    var buttonSaveCaption = ko.observable("");

    // #endregion

    // #region BEHAVIORS
    // initializers
    function activate() {
        loadList();
        setInitialDate();
    }
    function loadList() {
        $("#cashInTable").dataTable().fnDestroy();
        $('#cashInTable').DataTable({
            "ajax": {
                "url": RootUrl + "Administrator/CashIn/LoadList",
                "type": "GET",
                "datatype": "json"
            },
            "order": [[1, "desc"]],
            "columns": [
                { "data": "CashInId", "className": "hide" },
                {
                    "data": "CashInDate", "className": "dt-left",
                    "render": function(data, type, row) {
                        var pattern = /Date\(([^)]+)\)/;
                        var results = pattern.exec(row.date_trans);
                        var dt = new Date(parseFloat(results[1]));
                        return (dt.getMonth() + 1) + "/" + dt.getDate() + "/" + dt.getFullYear();
                    }
                },
                { "data": "UserName", "className": "dt-left" },
                { "data": "Amount", "className": "text-right", render: $.fn.dataTable.render.number(',', '.', 2, '') }
            ]
        });
    }

    function clearControls() {
        model.CashInId("");
        model.CashInDate("");
        model.UserName("");
        model.DateFrom("");
        model.DateTo("");
        model.Amount("");
    }

    function create() {
        isListShowed(false);
        isCreateShowed(true);
        buttonSaveCaption("Save");
        clearControls();
        getServerDate();
        getUser();
    }

    function viewItem(arg) {
        loaderApp.showPleaseWait();
        isListShowed(false);
        isCreateShowed(true);

        loaderApp.hidePleaseWait();
    }

    function backToList() {
        isListShowed(true);
        isCreateShowed(false);
        clearControls();
    }

    function save() {
        /*VALIDATIONS -START*/
        if (model.Amount() === "") {
            toastr.error("Amount is required.");
            model.Amount("");
            document.getElementById("Amount").focus();
            return false;
        }
        model.CashInDate(document.getElementById("CashInDate").val());
        /*VALIDATIONS -END*/

        loaderApp.showPleaseWait();
        var param = ko.toJS(model);
        var url = RootUrl + "Administrator/CashIn/Save";
        $.ajax({
            type: 'POST',
            url: url,
            data: ko.utils.stringifyJson(param),
            contentType: 'application/json; charset=utf-8',
            success: function (result) {
                if (result.success) {
                    swal("Success", result.message, "success");

                    swal({ title: "Success!", text: result.message, type: "success" }, function() { reloadPage() });

                    loaderApp.hidePleaseWait();
                } else {
                    loaderApp.hidePleaseWait();

                    swal("Error", result.message, "error");

                    clearControls();
                }
            }
        });
    }

    function reloadPage() {
        window.location.reload();
    }

    function setInitialDate() {
        $('.daterange-single').daterangepicker({
            singleDatePicker: true
        });
    }

    function getServerDate() {
        $.getJSON(RootUrl + "Administrator/CashIn/GetServerDate",
            function(result) {
                $('#CashInDate').val(result);
            });
    };

    function getUser() {
        $.getJSON(RootUrl + "Administrator/CashIn/GetUser",
            function(result) {
                $('#UserName').val(result);
            });
    };

    // #endregion

    var vm = {
        activate: activate,
        backToList: backToList,
        buttonSaveCaption: buttonSaveCaption,
        create: create,
        save: save,
        isListShowed: isListShowed,
        isCreateShowed: isCreateShowed,

        model: model
    };
    return vm;
})();
$(function () {
    "use strict";

    app.vm.activate();

    // Success
    $(".control-success").uniform({
        radioClass: 'choice',
        wrapperClass: 'border-success-600 text-success-800'
    });

    //Switchery
    var switches = Array.prototype.slice.call(document.querySelectorAll('.switchery'));
    switches.forEach(function (html) {
        var switchery = new Switchery(html, { color: '#4CAF50' });
    });

    ko.applyBindings(app.vm);
});