import 'package:flutter/material.dart';

class SgmButton extends StatelessWidget {
  final String label;
  final VoidCallback? onPressed;
  final bool isLoading;
  final IconData? icon;
  final bool fullWidth;
  final ButtonStyle? style;

  const SgmButton({
    super.key,
    required this.label,
    this.onPressed,
    this.isLoading = false,
    this.icon,
    this.fullWidth = true,
    this.style,
  });

  bool get _isDisabled => onPressed == null || isLoading;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Semantics(
      button: true,
      enabled: !_isDisabled,
      label: label,
      child: SizedBox(
        width: fullWidth ? double.infinity : null,
        height: 52,
        child: ElevatedButton(
          onPressed: _isDisabled ? null : onPressed,
          style: style ?? _defaultStyle(theme),
          child: AnimatedSwitcher(
            duration: const Duration(milliseconds: 200),
            transitionBuilder: (child, animation) =>
                FadeTransition(opacity: animation, child: child),
            child: isLoading
                ? const SizedBox(
                    key: ValueKey('loading'),
                    height: 22,
                    width: 22,
                    child: CircularProgressIndicator(
                      strokeWidth: 2.5,
                    ),
                  )
                : _Content(
                    key: const ValueKey('content'),
                    label: label,
                    icon: icon,
                  ),
          ),
        ),
      ),
    );
  }

  ButtonStyle _defaultStyle(ThemeData theme) {
    return ElevatedButton.styleFrom(
      minimumSize: const Size.fromHeight(52),
      textStyle: theme.textTheme.labelLarge?.copyWith(
        fontWeight: FontWeight.w600,
        letterSpacing: 0.5,
      ),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
      ),
    );
  }
}

class _Content extends StatelessWidget {
  final String label;
  final IconData? icon;

  const _Content({
    super.key,
    required this.label,
    this.icon,
  });

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        if (icon != null) ...[
          Icon(icon, size: 20),
          const SizedBox(width: 8),
        ],
        Flexible(
          child: Text(
            label,
            overflow: TextOverflow.ellipsis,
          ),
        ),
      ],
    );
  }
}