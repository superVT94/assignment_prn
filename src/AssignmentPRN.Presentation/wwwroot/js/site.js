"use strict";

document.addEventListener("DOMContentLoaded", () => {
    initializeConfirmations();
    initializeParticipantRows();
});

function initializeConfirmations() {
    document.querySelectorAll("form[data-confirm]").forEach((form) => {
        form.addEventListener("submit", (event) => {
            const message = form.dataset.confirm || "Bạn có chắc muốn thực hiện thao tác này?";
            if (!window.confirm(message)) {
                event.preventDefault();
            }
        });
    });
}

function initializeParticipantRows() {
    const form = document.getElementById("exam-session-form");
    const container = document.getElementById("participant-rows");
    const template = document.getElementById("participant-template");
    const addButton = document.getElementById("add-participant");
    const emptyMessage = document.getElementById("participant-empty");
    const validationMessage = document.getElementById("participant-validation");

    if (!form || !container || !template || !addButton) {
        return;
    }

    const renumberRows = () => {
        const rows = Array.from(container.querySelectorAll("[data-participant-row]"));
        rows.forEach((row, index) => {
            row.querySelectorAll("[data-field]").forEach((input) => {
                const field = input.dataset.field;
                input.name = `Participants[${index}].${field}`;
                input.id = `Participants_${index}_${field}`;
            });
            row.querySelectorAll("[data-label-for]").forEach((label) => {
                label.htmlFor = `Participants_${index}_${label.dataset.labelFor}`;
            });
            row.querySelectorAll("[data-valmsg-for]").forEach((element) => {
                const field = element.dataset.valmsgFor.split(".").pop();
                element.dataset.valmsgFor = `Participants[${index}].${field}`;
            });
            const heading = row.querySelector("[data-participant-number]");
            if (heading) {
                heading.textContent = `Lượt thi ${index + 1}`;
            }
            const removeButton = row.querySelector("[data-remove-participant]");
            if (removeButton) {
                removeButton.setAttribute("aria-label", `Xóa lượt thi ${index + 1}`);
            }
        });

        if (emptyMessage) {
            emptyMessage.classList.toggle("is-visible", rows.length === 0);
        }
    };

    addButton.addEventListener("click", () => {
        const fragment = template.content.cloneNode(true);
        const row = fragment.querySelector("[data-participant-row]");
        row.querySelectorAll("select").forEach((select) => {
            select.selectedIndex = 0;
            select.classList.remove("is-invalid");
        });
        row.querySelectorAll("input").forEach((input) => {
            input.classList.remove("is-invalid");
        });
        container.appendChild(fragment);
        renumberRows();
        const firstSelect = row.querySelector("select");
        if (firstSelect) {
            firstSelect.focus();
        }
    });

    container.addEventListener("click", (event) => {
        const button = event.target.closest("[data-remove-participant]");
        if (!button) {
            return;
        }

        const row = button.closest("[data-participant-row]");
        if (row) {
            row.remove();
            renumberRows();
        }
    });

    form.addEventListener("submit", (event) => {
        const rows = Array.from(container.querySelectorAll("[data-participant-row]"));
        if (validationMessage) {
            validationMessage.classList.remove("is-visible");
        }

        if (rows.length === 0) {
            event.preventDefault();
            event.stopImmediatePropagation();
            if (validationMessage) {
                validationMessage.textContent = "Phải có ít nhất một lượt thi.";
                validationMessage.classList.add("is-visible");
            }
            return;
        }

        const selectedStudents = rows
            .map((row) => Number(row.querySelector("select[data-field='StudentId']")?.value || 0))
            .filter((studentId) => studentId > 0);
        if (selectedStudents.length !== new Set(selectedStudents).size) {
            event.preventDefault();
            event.stopImmediatePropagation();
            if (validationMessage) {
                validationMessage.textContent = "Mỗi sinh viên chỉ được chọn một lần.";
                validationMessage.classList.add("is-visible");
            }
        }
    });

    renumberRows();
}
