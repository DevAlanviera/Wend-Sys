// Monobits Modal.  Versión: 1.3
// Estados e implementación
// última modificación: 15/jun/2018
// por Alejandro Quiñones Guzmán

$(document)
    .on("show.bs.modal",
        ".modal",
        function () {
            var zIndex = 1040 + (10 * $(".modal:visible").length);
            $(this).css("z-index", zIndex);
            setTimeout(function () {
                    $(".modal-backdrop").not(".modal-stack").css("z-index", zIndex - 1).addClass("modal-stack");
                },
                0);
    });

$(document)
    .on("shown.bs.modal",
        ".modal",
        function () {
            if ($(this).find('.select2') !== 'undefined') {
                $(this).find('.select2').each(function (i, obj) {
                    var $select = $(obj);
                    if (typeof $select.attr("placeholder") !== 'undefined') {
                        $select.select2({
                            placeholder: $select.attr('placeholder'),
                            allowClear: true
                        });
                    } else {
                        $select.select2();
                    }
                });
               
            }
        });

$(document)
    .on("hidden.bs.modal",
        ".modal",
        function (event) {
            if ($(".modal").hasClass("show")) {
                $("body").addClass("modal-open");
            }
    });

$(document)
    .on("hide.bs.modal",
        ".modal",
        function (event) {
            if ($(this).find('.datatable-server-side').length > 0) {
                $dataTables.pop();
            }
        });

// carga un modal por ajax y lo pone en el DOM, activa la forma si es que tiene
$(document).on("click",
    ".btn-modal-action",
    function (e) {
        e.preventDefault();
        var $btn = $(this);
        var modalId = $btn.attr("href");
        var $modal = $(modalId);
        var url = $modal.data("url");
        var $container = $modal;
        var id = $btn.data("id");

        if (typeof id !== "undefined") {
            url = url + "/" + id;
        }

        monoloading("show");

        $.get(url,
            function (data) {
                monoloading("hide");
                $container.html(data);              

                var $form = $modal.find("form");
                if (!$.isEmptyObject($form)) {
                    $.validator.unobtrusive.parse($form);
                }
                $container.find(".forma-ajax").ajaxForm(opcionesForma);
                $container.find(".forma-ajax--modal").ajaxForm(opcionesFormaModal);
                $container.find(".forma-ajax--modal-refresh").ajaxForm(opcionesFormaModalOld);

                // limpia el contenido cuando se cierra para evitar hacer referencia a elementos ocultos
                $modal.on('hidden.bs.modal',
                    function() {
                        $container.html(null);
                    });

                $modal.modal("show");
            }).fail(mostrarErrorSnack);
    });

function mostrarErrorSnack() {
    monoloading('hide');
    monotoast("Ocurrió un error inesperado");
}