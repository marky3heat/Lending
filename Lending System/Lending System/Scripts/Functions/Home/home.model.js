app.DashboardDisplays =
    function (released, collection, receivables) {
        "use strict";

        var self = this;

        // #region MODEL TO BE MAP
        self.Released = ko.observable(released);
        self.Collection = ko.observable(collection);
        self.Receivables = ko.observable(receivables);
        // #endregion
    };