app.vm = (function () {
    //"use strict";
 
    // #region CONTROLS                
    var isListShowed = ko.observable(true);
    var isCreateShowed = ko.observable(false);
    var forRestructureModel = new app.loanDetail();

    var buttonSaveCaption = ko.observable("");
    var forRestructure = ko.observable();
    // #endregion

    // #region BEHAVIORS
    // initializers
    function activate() {
        loadList();
        update();
    }

    function backToList() {
        isListShowed(true);
        isCreateShowed(false);
    }

    function update() {
        $.getJSON(RootUrl + "Home/GetForRestructureDues", function (result) {
            forRestructure(result);
        });
    }

    function loadList() {
        $("#loan-table").dataTable().fnDestroy();
        $('#loan-table').DataTable({
            "ajax": {
                "url": RootUrl + "Administrator/Restructure/LoadList",
                "type": "GET",
                "datatype": "json"
            },
            "order": [[1, "desc"]],
            "columns": [
                { "data": "autonum", "className": "hide" },
                { "data": "customer_name", "className": "dt-left" },
                { "data": "loan_no", "className": "dt-left" },
                { "data": "due_date", "className": "dt-left" },       
                { "data": "balance", "className": "text-right", render: $.fn.dataTable.render.number(',', '.', 2, '') },
                {
                    "render": function (data, type, row) {
                        return '<ul class="icons-list">' +
					    '<li class="text-primary-600">' +
                        '<a onclick="app.vm.restructureLoan(' + row.autonum + ')" href="javascript:void(0);"><i class="icon-pencil7"></i></a>' +
                        '</li>' +
						'</ul>';
                    }
                },
            ]
        });
    }

    function restructureLoan(arg) {
        swal({
            title: 'Are you sure?',
            text: "You won't be able to revert this!",
            type: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#3085d6',
            cancelButtonColor: '#d33',
            confirmButtonText: 'Yes, proceed to restructure'
        }).then(function (result) {
            if (result.dismiss != "cancel") {
                isListShowed(false);
                isCreateShowed(true);

                $.getJSON(RootUrl + "Administrator/Restructure/GetLoanDetail?id=" + arg, function (result) {
                    forRestructureModel.autonum(result[0].autonum);
                    forRestructureModel.customer_name(result[0].customer_name);
                    forRestructureModel.loan_no(result[0].loan_no);
                    forRestructureModel.loan_granted(result[0].loan_granted);
                    forRestructureModel.loan_interest_rate(result[0].loan_interest_rate);
                    forRestructureModel.payment_scheme(result[0].payment_scheme);
                    forRestructureModel.due_date(result[0].due_date);
                    forRestructureModel.loan_date(result[0].loan_date);
                    forRestructureModel.installment_no(result[0].installment_no);
                    forRestructureModel.total_receivables(result[0].total_receivables);
                    forRestructureModel.balance(result[0].balance);
                    forRestructureModel.restructured_interest(result[0].restructured_interest);
                    forRestructureModel.new_balance(result[0].new_balance);
                });
            }
        })
    }

    function save() {
        debugger;
        if (forRestructureModel.loan_no() != "" && forRestructureModel.loan_no() != undefined) {
            var id = forRestructureModel.loan_no();

            loaderApp.showPleaseWait();
            var url = RootUrl + "Administrator/Restructure/Save?id=" + id;
            $.ajax({
                type: 'POST',
                url: url,
                contentType: 'application/json; charset=utf-8',
                success: function (result) {
                    if (result.success) {
                        swal({ title: "Success!", text: result.message, type: "success" });

                        loaderApp.hidePleaseWait();
                        loadList();
                        backToList();
                    } else {
                        loaderApp.hidePleaseWait();

                        swal("Error", result.message, "error");
                    }
                }
            });
        }
    }

    // #endregion

    var vm = {
        activate: activate,
        backToList: backToList,
        update: update,
        isListShowed: isListShowed,
        isCreateShowed: isCreateShowed,
        forRestructure: forRestructure,
        forRestructureModel: forRestructureModel,
        restructureLoan: restructureLoan,
        save: save
    };
    return vm;
})();
$(function () {
    "use strict";

    app.vm.activate();

    $.wait = function (callback, seconds) {
        return window.setTimeout(callback, seconds * 1000);
    }

    if (ForRestructure) {

        app.vm.forRestructure(ForRestructure)

        
    }
    else {
        app.vm.update();
    }

    ko.applyBindings(app.vm);
});