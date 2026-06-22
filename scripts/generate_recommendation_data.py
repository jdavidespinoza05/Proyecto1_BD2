from __future__ import annotations

import csv
from datetime import date, timedelta
from pathlib import Path


ROOT_DIR = Path(__file__).resolve().parents[1]
RAW_DATA_DIR = ROOT_DIR / "spark" / "data" / "raw_data"

# Red determinista de recomendaciones.
# La llave es quien recomienda y la lista contiene
# los usuarios recomendados.
RECOMMENDATIONS: dict[int, list[int]] = {
    1: [2, 3, 4, 5, 6],
    2: [1, 3, 5],
    3: [2, 5, 7, 8],
    4: [1, 5, 6],
    5: [2, 3, 9, 10],
    6: [1, 2, 3],
    7: [1, 2, 8],
    8: [1, 3, 5],
    9: [1, 2, 5],
    10: [1, 3, 5],
    11: [1, 2],
    12: [1, 3],
    13: [1, 5],
    14: [1, 2],
    15: [1, 3],
}


def read_user_ids() -> set[int]:
    users_path = RAW_DATA_DIR / "users.csv"

    if not users_path.exists():
        raise FileNotFoundError(
            f"No se encontró el archivo requerido: {users_path}"
        )

    with users_path.open(
        mode="r",
        newline="",
        encoding="utf-8-sig",
    ) as csv_file:
        return {
            int(row["id"])
            for row in csv.DictReader(csv_file)
        }


def main() -> None:
    user_ids = read_user_ids()
    rows: list[dict[str, object]] = []
    existing_pairs: set[tuple[int, int]] = set()

    base_date = date(2025, 1, 1)
    recommendation_id = 1

    for recommender_id, recommended_ids in RECOMMENDATIONS.items():
        if recommender_id not in user_ids:
            raise ValueError(
                f"El usuario recomendador {recommender_id} no existe."
            )

        for recommended_id in recommended_ids:
            if recommended_id not in user_ids:
                raise ValueError(
                    f"El usuario recomendado {recommended_id} no existe."
                )

            if recommender_id == recommended_id:
                raise ValueError(
                    "Un usuario no puede recomendarse a sí mismo."
                )

            pair = (recommender_id, recommended_id)

            if pair in existing_pairs:
                raise ValueError(
                    f"Recomendación duplicada detectada: {pair}."
                )

            existing_pairs.add(pair)

            rows.append(
                {
                    "id": recommendation_id,
                    "recommender_user_id": recommender_id,
                    "recommended_user_id": recommended_id,
                    "recommendation_date": (
                        base_date + timedelta(days=recommendation_id * 7)
                    ).isoformat(),
                }
            )

            recommendation_id += 1

    output_path = RAW_DATA_DIR / "recommendations.csv"

    with output_path.open(
        mode="w",
        newline="",
        encoding="utf-8-sig",
    ) as csv_file:
        writer = csv.DictWriter(
            csv_file,
            fieldnames=[
                "id",
                "recommender_user_id",
                "recommended_user_id",
                "recommendation_date",
            ],
        )
        writer.writeheader()
        writer.writerows(rows)

    print(f"recommendations.csv: {len(rows)} registros generados")
    print("Validación de recomendaciones completada correctamente.")


if __name__ == "__main__":
    main()