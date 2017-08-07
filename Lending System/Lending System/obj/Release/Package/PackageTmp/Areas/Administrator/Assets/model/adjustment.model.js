app.createAdjustmentModel = function () {
    "use strict";

    var self = this;

    // #region MODEL TO CREATE/UPDATE
    self.Autonum = ko.observable();
    self.DateTrans = ko.observable();
    self.TransType = ko.observable();
    self.LoanNo = ko.observable();
    self.Amount = ko.observable();
    self.Remarks = ko.observable();
    // #endregion   

    return self;
};