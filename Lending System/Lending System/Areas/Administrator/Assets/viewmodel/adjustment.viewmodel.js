app.vm = (function() {
    //"use strict";
    var model = new app.createAdjustmentModel();

    // #region CONTROLS                
    var isAdjustmentListShowed = ko.observable(true);
    var isCreateModeShow = ko.observable(false);
    var buttonCaption = ko.observable("");

    var adjustmentNo = ko.observable("");

    // #endregion

    // #region BEHAVIORS
    // initializers
    function clearControls() {
        model.DateTrans("");
        model.TransType("");
        model.LoanNo("");
        model.Amount("");
        model.Remarks("");
    }

    function activate() {
        loadList();
    }

    function create() {
        isAdjustmentListShowed(false);
        isCreateModeShow(true);
        buttonCaption("Save");

        getServerDate();
        clearControls();
        getAdjustmentNo();
        SetInitialDate();
    }

    function save() {
        /*VALIDATIONS -START*/
        model.Autonum = $('#AdjustmentNo').val();

        if (model.DateTrans().trim() === "") {
            toastr.error("Date is required.");
            model.DateTrans("");
            document.getElementById("DateTrans").focus();
            return false;
        }

        if ($('input[name=radio-inline-left]:checked').length <= 0) {
            // do something here
            toastr.error("Select an adjustment type.");
            model.TransType("");
            document.getElementById("TransTypeDebitMemo").focus();
            return false;
        }
        if (document.getElementById('TransTypeDebitMemo').checked) {
            //Male radio button is checked
            model.TransType = "Debit memo";
        } else if (document.getElementById('TransTypeCreditMemo').checked) {
            //Female radio button is checked
            model.TransType = "Credit memo";
        }

        model.LoanNo = $("#LoanNo").find('option:selected').text();
        if (model.LoanNo === "") {
            toastr.error("Select loan no.");
            model.LoanNo("");
            document.getElementById("LoanNo").focus();
            return false;
        }

        if (model.Amount().trim() === "") {
            toastr.error("Amount is required.");
            model.Amount("");
            document.getElementById("Amount").focus();
            return false;
        }

        loaderApp.showPleaseWait();
        var param = ko.toJS(model);
        var url = RootUrl + "Administrator/Adjustment/Save";
        $.ajax({
            type: 'POST',
            url: url,
            data: ko.utils.stringifyJson(param),
            contentType: 'application/json; charset=utf-8',
            success: function (result) {
                if (result.success) {

                    swal({ title: "Success!", text: result.message, type: "success" }, function () { reloadPage() });

                    loaderApp.hidePleaseWait();
                } else {
                    loaderApp.hidePleaseWait();

                    swal("Error", result.message, "error");
                }
            }
        });
    }

    function backToList() {
        isAdjustmentListShowed(true);
        isCreateModeShow(false);
    }

    function loadList() {
        $("#adjustmentListTable").dataTable().fnDestroy();
        $('#adjustmentListTable').DataTable({
            "ajax": {
                "url": RootUrl + "Administrator/Adjustment/LoadList",
                "type": "GET",
                "datatype": "json"
            },
            "order": [[2, "desc"]],
            "columns": [
                { "data": "Autonum", "className": "hide" },
                {
                    "data": "DateTrans",
                    "className": "text-left",
                    "render": function(data, type, row) {
                        var pattern = /Date\(([^)]+)\)/;
                        var results = pattern.exec(row.DateTrans);
                        var dt = new Date(parseFloat(results[1]));
                        return (dt.getMonth() + 1) + "/" + dt.getDate() + "/" + dt.getFullYear();
                    }
                },
                { "data": "LoanNo", "className": "text-left" },
                {
                    "data": "Amount",
                    "className": "text-right",
                    render: $.fn.dataTable.render.number(',', '.', 2, '')
                },
                { "data": "Remarks", "className": "text-left" },
                {
                    "render": function(data, type, row) {
                        return '<ul class="icons-list">' +
                            '<li class="dropdown">' +
                            '<a href="#" class="dropdown-toggle" data-toggle="dropdown"><i class="icon-menu7"></i></a>' +
                            '<ul class="dropdown-menu dropdown-menu-right">' +
                            '<li><a href="#"><i class="icon-file-stats"></i> View adjustment</a></li>' +
                            '</ul>' +
                            '</li>' +
                            '</ul>';
                    }
                }
            ]
        });
    }
    function getAdjustmentNo() {
        $.getJSON(RootUrl + "Administrator/Adjustment/GetAdjustmentNo", function (result) {
            adjustmentNo(result);
        });
    }

    function getServerDate() {
        $.getJSON(RootUrl + "Administrator/Adjustment/GetServerDate",
            function(result) {
                $('#PaymentDate').val(result);
            });
    };

    function hideSidebar() {
        $('#hide-sidebar').trigger('click');
    };

    function reloadPage() {
        window.location.reload();
    }

    function SetInitialDate() {
        $('.daterange-single').daterangepicker({
            singleDatePicker: true
        });
    }

    var vm = {
        activate: activate,
        create: create,
        save: save,
        backToList: backToList,

        isAdjustmentListShowed: isAdjustmentListShowed,
        isCreateModeShow: isCreateModeShow,

        model: model,
        buttonCaption: buttonCaption,
        SetInitialDate: SetInitialDate,
        adjustmentNo: adjustmentNo
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

    $('.select').select2();

    ko.applyBindings(app.vm);
});
