$(function () {
    $(".forma-ajax--modal").ajaxForm(opcionesFormaModal);
    $(".forma-ajax").ajaxForm(opcionesForma);
    $(".forma-ajax--modal-refresh").ajaxForm(opcionesFormaModalOld);
});

var opcionesFormaModal = {
    beforeSubmit: prepararPeticion,
    success: ejecutarRespuesta,
    error: mostrarError
};

var opcionesFormaModalOld = {
    beforeSubmit: prepararPeticion,
    success: ejecutarRespuestaOld,
    error: mostrarError
};

var opcionesForma = {
    beforeSubmit: prepararPeticion,
    success: ejecutarRespuestaForma,
    error: mostrarError
};


function prepararPeticion(arr, $form, options) {
    // valida la forma, si pasa continua el submit
    $form.validate();
    var button = $form.find('button[type="submit"]');
    $form.find(".modal-errores").html("");

    if ($form.valid()) {
        monoloading('show');
        button.prop("disabled", true);
    } else {
        Console.log("error");
        $form.find(".modal-errores").html("Error");
        button.prop("disabled", false);
        return false;
    }
}

function ejecutarRespuesta(responseText, statusText, xhr, $form) {
    var json = responseText;
    var $modal = $form.parents(".modal");

    $modal.find('button[type="submit"]').prop("disabled", false);
    monoloading('hide');

    $modal.find(".modal-errores").html("");
    if (json.status === "ok") {
        $modal.modal('hide');
        monotoast(json.body);

        attemptRefreshGrids($form); 
    } else {
        $modal.find(".modal-errores").html(`<p>${json.body}</p>`);
    }
}

function attemptRefreshGrids($form) {
    var willRefreshGrids = $form.data('refresh-grids');

    if (willRefreshGrids == true) {
        refreshGrids();
    } else if (typeof $form.find('.datatable') !== 'undefined') {
        document.getElementsByClassName('datatable')[document.getElementsByClassName('datatable').length - 1].ej2_instances[0].refresh();
    }
}

function refreshGrids() {
    var gridCount = document.getElementsByClassName('datatable').length;

    for (var x = 0; x < gridCount; x++) {
        document.getElementsByClassName('datatable')[x].ej2_instances[0].refresh();
    }
}

function ejecutarRespuestaForma(responseText, statusText, xhr, $form) {
    var json = responseText;

    $form.find('button[type="submit"]').prop("disabled", false);
    monoloading('hide');

    $form.find(".modal-errores").html("");
    if (json.status === "ok") {
        $form.resetForm();
        attemptRefreshGrids($form); 

        monotoast(json.body);
    } else {
        $form.find(".modal-errores").html(`<p>${json.body}</p>`);
    }
}


function ejecutarRespuestaOld(responseText, statusText, xhr, $form) {
    var json = responseText;
    var $modal = $form.parents(".modal");

    $modal.find(".modal-errores").html("");
    if (json.status === "ok") {
        Cookies.set("consola-toast", json.body);
        window.location.reload();
    } else {
        monoloading('hide');
        $modal.find('button[type="submit"]').prop("disabled", false);
        $modal.find(".modal-errores").html(`<p>${json.body}</p>`);
    }
}

function mostrarError(xhr, statusText, errorThrown, $form) {
    $form.find('[type="submit"]').prop("disabled", false);
    monoloading('hide');
    if (xhr.status === 401) {
        $form.find(".modal-errores")
            .html("<p>Tu sesión caducó, por favor <a href='/' target='_blank'>inicia sesión</a> e intenta de nuevo</p>");
    } else if (xhr.status === 500) {
        $form.find(".modal-errores")
            .html("<p>Ocurrió un error en el servidor</p>");
    } else {
        $form.find(".modal-errores")
            .html("<p>Ocurrió un error inesperado</p>");
    }
}
